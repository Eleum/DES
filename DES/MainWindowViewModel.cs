using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DES
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private const int
            SOURCEBLOCKSIZE = 64,
            CHARSIZE = 8,
            KEYBLOCKSIZE = 56,
            ROUNDS = 16;

        private ObservableCollection<string> _inputText, _inputKey, _initialPermutationText, _afterJoin, _result;

        private string _inputTextField, _inputKeyField, _resultField, _isEncrypt;

        private bool _isDefaultKey, _isHexText, _isHexKey;

        public ObservableCollection<string> InputText
        {
            get
            {
                return _inputText;
            }
            set
            {
                _inputText = value;
                OnPropertyChanged("InputText");
            }
        }

        public ObservableCollection<string> InputKey
        {
            get
            {
                return _inputKey;
            }
            set
            {
                _inputKey = value;
                OnPropertyChanged("InputKey");
            }
        }

        public ObservableCollection<string> InitialPermutationText
        {
            get
            {
                return _initialPermutationText;
            }
            set
            {
                _initialPermutationText = value;
                OnPropertyChanged("InitialPermutationText");
            }
        }

        public ObservableCollection<string> AfterJoin
        {
            get
            {
                return _afterJoin;
            }
            set
            {
                _afterJoin = value;
                OnPropertyChanged("AfterJoin");
            }
        }

        public ObservableCollection<string> Result
        {
            get
            {
                return _result;
            }
            set
            {
                _result = value;
                OnPropertyChanged("Result");
            }
        }

        public string InputTextField
        {
            get
            {
                return _inputTextField;
            }
            set
            {
                _inputTextField = value;
                OnPropertyChanged("InputTextField");
            }
        }

        public string InputKeyField
        {
            get
            {
                return _inputKeyField;
            }
            set
            {
                _inputKeyField = value;
                OnPropertyChanged("InputKeyField");
            }
        }

        public string ResultField
        {
            get
            {
                return _resultField;
            }
            set
            {
                _resultField = value;
                OnPropertyChanged("ResultField");
            }
        }

        public bool IsDefaultKey
        {
            get
            {
                return _isDefaultKey;
            }
            set
            {
                _isDefaultKey = value;
                OnPropertyChanged("IsDefaultKey");
            }
        }

        public bool IsHexText
        {
            get
            {
                return _isHexText;
            }
            set
            {
                _isHexText = value;
                OnPropertyChanged("IsHexText");
            }
        }

        public bool IsHexKey
        {
            get
            {
                return _isHexKey;
            }
            set
            {
                _isHexKey = value;
                OnPropertyChanged("IsHexKey");
            }
        }

        public string IsEncrypt
        {
            get
            {
                return _isEncrypt;
            }
            set
            {
                _isEncrypt = value;
                OnPropertyChanged("IsEncrypt");
            }
        }

        public int Type { get; private set; }

        private ObservableCollection<RoundInfo> _info;
        public ObservableCollection<RoundInfo> Info
        {
            get
            {
                return _info;
            }
            set
            {
                _info = value;
                OnPropertyChanged("Info");
            }
        }

        private ObservableCollection<RoundInfo> _infoDecoded;
        public ObservableCollection<RoundInfo> InfoDecoded
        {
            get
            {
                return _infoDecoded;
            }
            set
            {
                _infoDecoded = value;
                OnPropertyChanged("InfoDecoded");
            }
        }

        private string[] roundKeys = new string[16];
        private string[,] LRs = new string[2, 17];

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public ICommand EncryptCommand => new RelayCommand(o => StartOperation(0));
        public ICommand DecryptCommand => new RelayCommand(o => StartOperation(1));
        public ICommand BrowseFilesCommand => new RelayCommand(o => BrowseFiles());
        public ICommand SaveToFileCommand => new RelayCommand(o => SaveToFile());

        public MainWindowViewModel()
        {
            Info = new ObservableCollection<RoundInfo>();
            InfoDecoded = new ObservableCollection<RoundInfo>();

            InputText = new ObservableCollection<string> { "", "" };
            InputKey = new ObservableCollection<string> { "", "" };
            InitialPermutationText = new ObservableCollection<string> { "", "" };
            AfterJoin = new ObservableCollection<string> { "", "" };
            Result = new ObservableCollection<string> { "", "" };
        }

        private IEnumerable<string> CompleteAndDivide(string text, InputMode mode)
        {
            switch (mode)
            {
                case InputMode.PlainText:
                    while ((text.Length * CHARSIZE) % SOURCEBLOCKSIZE != 0)
                    {
                        text += "\0";
                    }
                    break;

                case InputMode.Hex:
                    while ((text.Length * CHARSIZE / 2) % SOURCEBLOCKSIZE != 0)
                    {
                        text += "0";
                    }
                    break;
            }

            var blocksCount = -1;
            switch (mode)
            {
                case InputMode.PlainText:
                    var binaryString = ToBinary(text);

                    blocksCount = binaryString.Length / SOURCEBLOCKSIZE;

                    if (binaryString.Length / SOURCEBLOCKSIZE != (double)binaryString.Length / SOURCEBLOCKSIZE)
                    {
                        blocksCount = binaryString.Length / SOURCEBLOCKSIZE + 1;
                        binaryString = binaryString.PadRight(blocksCount * SOURCEBLOCKSIZE, '0');
                    }

                    for (int i = 0; i < blocksCount; i++)
                    {
                        yield return string.Join("", binaryString.Substring(i * SOURCEBLOCKSIZE, SOURCEBLOCKSIZE));
                    }
                    break;

                case InputMode.Hex:
                    blocksCount = text.Length * CHARSIZE / SOURCEBLOCKSIZE / 2;
                    for (int i = 0; i < blocksCount; i++)
                    {
                        yield return string.Join("",
                            text.Substring(i * CHARSIZE * 2, CHARSIZE * 2)
                            .Select(x => HexToBinary(x.ToString())));
                    }
                    break;
            }
        }

        private void StartOperation(int type)
        {
            if (!InitialErrorCheck()) return;

            Type = type;
            GenerateRoundKeys();

            var result = string.Empty;
            InputText[type] = InputTextField;
            var text = CompleteAndDivide(InputTextField, IsHexText ? InputMode.Hex : InputMode.PlainText);

            switch (type)
            {
                case 0:
                    text = text.Where(x => x.Distinct().Count() != 1);

                    foreach (var block in text)
                    {
                        var encodedBlock = EncodeBlock(block);

                        var encodedPart = string.Join("",
                            encodedBlock.SplitByNChars(CHARSIZE)
                            .Select(x => Convert.ToInt32(x, 2))
                            .Select(x =>
                            {
                                var temp = x.ToString("X");
                                return temp.Length == CHARSIZE / 4 ? temp : "0" + temp;
                            })
                        );
                        result += encodedPart;
                    }

                    Result[type] = ResultField = result;
                    IsEncrypt = "yes";
                    break;
                case 1:
                    ResultField = string.Empty;
                    var resultHex = string.Empty;
                    var resultPlain = string.Empty;
                    var hexString = string.Empty;

                    foreach (var block in text)
                    {
                        var decodedBlock = DecodeBlock(block);

                        hexString += string.Join("",
                            decodedBlock.SplitByNChars(8)
                            .Select(x => Convert.ToInt32(x, 2).ToString("X").PadLeft(2, '0'))
                        );
                    }

                    resultPlain += HexToString(hexString);
                    Result[type] = resultPlain;
                    ResultField = $"Hex: {hexString}\r\n\r\nPlain text: {resultPlain}";
                    IsEncrypt = "no";
                    break;
            }
            return;
        }

        /// <summary>
        /// Translates a single char into its binary form
        /// </summary>
        /// <param name="char"></param>
        /// <returns></returns>
        private string CharToBinary(char @char)
        {
            //TODO: переписать на padleft
            var ch = Convert.ToString(@char, 2);

            while (ch.Length < CHARSIZE)
            {
                ch = "0" + ch; // add up to 8 bits if needed
            }

            return ch;
        }

        private static string ToBinary(string source)
        {
            var byteSource = Encoding.UTF8.GetBytes(source);
            return string.Concat(byteSource.Select(x =>
            {
                var binary = Convert.ToString(x, 2).PadLeft(CHARSIZE, '0');
                return binary;
            }));
        }

        public static string HexToString(string source)
        {
            var bytes = new byte[source.Length / 2];

            for (int i = 0; i < source.Length / 2; i++)
            {
                bytes[i] = Convert.ToByte(source.Substring(i * 2, 2), 16);
            }
            return Encoding.UTF8.GetString(bytes.Where(x => x != 0).ToArray());
        }

        private string HexToBinary(string hexString)
        {
            return Convert.ToString(Convert.ToInt32(hexString, 16), 2).PadLeft(4, '0');
        }

        public static string BinaryToHex(string binString)
        {
            var result = string.Empty;

            for (int i = 0; i < binString.Length; i += CHARSIZE)
            {
                var temp = Convert.ToString(Convert.ToInt32(binString.Substring(i, CHARSIZE), 2), 16).PadLeft(2, '0').ToUpper();
                result += temp;
            }

            return result;
        }

        private string InitialPermutation(string text)
        {
            return Transpose(TransposeType.InitialPermutation, text, ManagerMatrix.GetInitialPermutationMatrix());
        }

        private string BinaryKey(string keyword, InputMode mode)
        {
            IEnumerable<string> key;

            if (mode == InputMode.Hex)
            {
                key = keyword.Select(x => HexToBinary(x.ToString()));
            }
            else
            {
                key = keyword.Select(x => CharToBinary(x));
            }

            return string.Join("", key);
        }

        public static string GenerateKey()
        {
            var random = new Random();

            int[] bits = new int[64];

            for (int i = 0; i < 64; i++)
            {
                if ((i + 1) % 8 == 0)
                {
                    var onesCount = bits
                        .Skip(((i + 1) / 8 - 1) * 8)
                        .Take(7)
                        .Aggregate(0, (prev, next) => prev + next);
                    bits[i] = onesCount % 2 == 0 ? 1 : 0;
                    continue;
                }

                bits[i] = random.Next(0, 2);
            }

            return bits.Aggregate("", (prev, next) => prev + next);
        }

        /// <summary>
        /// Transposing bytes in <paramref name="source"/> according to defined <paramref name="matrix"/>
        /// </summary>
        /// <param name="type">type of transposing</param>
        /// <param name="source">text to transpose</param>
        /// <param name="matrix">matrix used to transpose</param>
        /// <returns></returns>
        private string Transpose(TransposeType type, string source, int[,] matrix)
        {
            char[] temp = null;
            int N = -1, M = -1; // rows and cols 

            switch (type)
            {
                case TransposeType.InitialPermutation:
                case TransposeType.FinalPermutation:
                    temp = new char[64];
                    N = M = 8;
                    break;
                case TransposeType.CompressedKey:
                    temp = new char[56];
                    N = 4; M = 14;
                    break;
                case TransposeType.RoundKey:
                    temp = new char[48];
                    N = 6; M = 8;
                    break;
                case TransposeType.ExpandedBlock:
                    temp = new char[48];
                    N = 8; M = 6;
                    break;
                case TransposeType.SBoxPermutation:
                    temp = new char[32];
                    N = 8; M = 4;
                    break;
            }

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < M; j++)
                {
                    temp[i * M + j] = source[matrix[i, j] - 1];
                }
            }

            return temp.Aggregate("", (prev, next) => prev + next);
        }

        private void Swap(ref char a, ref char b)
        {
            var temp = a;
            a = b;
            b = temp;
        }

        private string Shift(string key, int times, Direction direction = Direction.Left)
        {
            if (direction == Direction.Left)
            {
                for (int i = 0; i < times; i++)
                {
                    key = key.Substring(1, key.Length - 1) + key.Substring(0, 1);
                }
            }
            else
            {
                for (int i = 0; i < times; i++)
                {
                    key = key.Substring(key.Length - 1) + key.Substring(0, key.Length - 1);
                }
            }

            return key;
        }

        /// <summary>
        /// XOR operation of bits between key and expanded block followed by SBox operations and its permutation
        /// </summary>
        /// <param name="block">Expanded block of (n-1) round</param>
        /// <param name="key">Round key</param>
        /// <returns></returns>
        private string FFunction(string block, string key)
        {
            var expression = XOROperation(key, block);
            var SBoxedExpression = SBoxOperation(expression);
            return SBoxPermutation(SBoxedExpression);
        }

        private string ProcessSBox(string text, int[,] sbox)
        {
            int row = Convert.ToInt32(text[0].ToString() + text[5].ToString(), 2);
            int col = Convert.ToInt32(text.Substring(1, 4), 2);

            var result = Convert.ToString(sbox[row, col], 2);

            while (result.Length < 4)
            {
                result = "0" + result;
            }

            return result;
        }

        private string SBoxOperation(string expression)
        {
            var result = string.Empty;

            for (int i = 0; i < 8; i++)
            {
                result += ProcessSBox(expression.Substring(i * 6, 6), ManagerMatrix.GetSBoxMatrix(i + 1));
            }

            return result;
        }

        private string XOROperation(string left, string right)
        {
            var output = new char[left.Length];

            for (int i = 0; i < left.Length; i++)
            {
                output[i] = left[i].Equals(right[i]) ? '0' : '1';
            }

            return string.Join("", output);
        }

        private string SBoxPermutation(string expression)
        {
            return Transpose(TransposeType.SBoxPermutation, expression, ManagerMatrix.GetSBoxPermutationMatrix());
        }

        private string EncodeBlock(string block)
        {
            InitialPermutationText[Type] = InitialPermutation(block);

            LRs[0, 0] = InitialPermutationText[Type].Substring(0, InitialPermutationText[Type].Length / 2);
            LRs[1, 0] = InitialPermutationText[Type].Substring(InitialPermutationText[Type].Length / 2);

            InitialPermutationText[Type] = $"Initial permutation: {BinaryToHex(InitialPermutationText[Type])}\n" +
                $"Parts: L0 = {BinaryToHex(LRs[0, 0])}; R0 = {BinaryToHex(LRs[1, 0])}";

            GetLeftRight(Info);

            var blockToEncode = LRs[1, 16] + LRs[0, 16];
            AfterJoin[Type] = BinaryToHex(blockToEncode);

            return Transpose(TransposeType.FinalPermutation, blockToEncode, ManagerMatrix.GetFinalPermutationMatrix());
        }

        private string DecodeBlock(string block)
        {
            InitialPermutationText[Type] = InitialPermutation(block);

            LRs[0, 0] = InitialPermutationText[Type].Substring(0, InitialPermutationText[Type].Length / 2);
            LRs[1, 0] = InitialPermutationText[Type].Substring(InitialPermutationText[Type].Length / 2);

            InitialPermutationText[Type] = $"Initial permutation: {BinaryToHex(InitialPermutationText[Type])}\n" +
                $"Parts: L0 = {BinaryToHex(LRs[0, 0])}; R0 = {BinaryToHex(LRs[1, 0])}";

            GetLeftRight(InfoDecoded, true);

            var blockToDecode = LRs[1, 16] + LRs[0, 16];
            AfterJoin[Type] = BinaryToHex(blockToDecode);

            return Transpose(TransposeType.FinalPermutation, blockToDecode, ManagerMatrix.GetFinalPermutationMatrix());
        }

        private void GetLeftRight(ObservableCollection<RoundInfo> info, bool isReverse = false)
        {
            for (int i = 1; i < ROUNDS + 1; i++)
            {
                LRs[0, i] = LRs[1, i - 1];

                var expandedBlock = Transpose(TransposeType.ExpandedBlock, LRs[1, i - 1], ManagerMatrix.GetExpandedBlockMatrix());
                var expression = FFunction(expandedBlock, isReverse ? roundKeys[ROUNDS - i] : roundKeys[i - 1]);

                LRs[1, i] = XOROperation(LRs[0, i - 1], expression);

                info.Add(new RoundInfo
                {
                    RoundNo = i,
                    LeftPart = BinaryToHex(LRs[0, i]),
                    RightPart = BinaryToHex(LRs[1, i]),
                    RoundKey = BinaryToHex(roundKeys[i - 1])
                });
            }
        }

        private void GenerateRoundKeys()
        {
            InputKey[Type] = InputKeyField;
            var initialKey = BinaryKey(InputKeyField, IsHexKey ? InputMode.Hex : InputMode.PlainText);

            var key = Transpose(TransposeType.CompressedKey, initialKey, ManagerMatrix.GetCompressedKeyMatrix());

            var keyParts = new string[17, 2];
            keyParts[0, 0] = key.Substring(0, key.Length / 2);
            keyParts[0, 1] = key.Substring(key.Length / 2);

            var oneTimeShift = new[] { 1, 2, 9, 16 };

            for (int i = 1; i < ROUNDS + 1; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    keyParts[i, j] = Shift(keyParts[i - 1, j], oneTimeShift.Contains(i) ? 1 : 2);
                }
            }

            for (int i = 0; i < ROUNDS; i++)
            {
                roundKeys[i] = Transpose(
                    TransposeType.RoundKey,
                    string.Join("", keyParts[i + 1, 0], keyParts[i + 1, 1]), // +1 coz this this array contains 17 elements
                    ManagerMatrix.GetPBoxCompressMatrix()
                );
            }
        }

        private void BrowseFiles()
        {
            var dialog = new OpenFileDialog()
            {
                DefaultExt = ".txt",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true) return;

            string fileName = dialog.FileName;
            var parts = fileName.Split('.');
            if (parts[parts.Length - 1] != "txt")
            {
                MessageBox.Show("Only .txt files are allowed", "File extension error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            InputTextField = File.ReadAllText(fileName, Encoding.UTF8);
        }

        private void SaveToFile()
        {
            if(string.IsNullOrEmpty(ResultField))
            {
                MessageBox.Show("There's nothing to write to the file", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dialog = new SaveFileDialog()
                {
                    DefaultExt = ".txt",
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
                };

                if (dialog.ShowDialog() != true) return;

                string fileName = dialog.FileName;
                File.WriteAllText(fileName, ResultField);
                MessageBox.Show("File saved", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool InitialErrorCheck()
        {
            if (string.IsNullOrWhiteSpace(InputTextField) || string.IsNullOrWhiteSpace(InputKeyField))
            {
                MessageBox.Show("Text or key for the operation cannot be empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (IsHexText && !Regex.IsMatch(InputTextField, @"\A\b[0-9a-fA-F]+\b\Z"))
            {
                MessageBox.Show("Text is not valid hexadecimal string", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (IsHexKey && !Regex.IsMatch(InputKeyField, @"\A\b[0-9a-fA-F]+\b\Z"))
            {
                MessageBox.Show("Key is not valid hexadecimal string", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if ((IsHexKey && InputKeyField.Length != 16) || (!IsHexKey && InputKeyField.Length != 8))
            {
                MessageBox.Show("The key has to be 8 bytes long (8 chars or 16 chars for HEX)", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }
    }
}