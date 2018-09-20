using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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

        private string _inputText, _inputKey, _textAndKey, 
            _initialPermutationText, _afterJoin, _result, _resultField, 
            _isEncrypt;

        private bool _isDefaultKey;

        public string InputText
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

        public string InputKey
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

        public string TextAndKey
        {
            get
            {
                return _textAndKey;
            }
            set
            {
                _textAndKey = value;
                OnPropertyChanged("TextAndKey");
            }
        }

        public string InitialPermutationText
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

        public string AfterJoin
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

        public string Result
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

        private ObservableCollection<RoundInfo> _infoEncoded;
        public ObservableCollection<RoundInfo> InfoEncoded
        {
            get
            {
                return _infoEncoded;
            }
            set
            {
                _infoEncoded = value;
                OnPropertyChanged("InfoEncoded");
            }
        }

        public ObservableCollection<RoundInfo> InfoDecoded { get; set; }

        private string[] roundKeys = new string[16];
        private string[,] LRs = new string[2, 17];

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public ICommand EncryptCommand => new RelayCommand(o => StartOperation(0));
        public ICommand DecryptCommand => new RelayCommand(o => StartOperation(1));

        //TODO: divide this into multiple functions
        private IEnumerable<string> CompleteAndDivide(string text, InputMode mode)
        {
            var blocksCount = -1;

            switch (mode)
            {
                case InputMode.PlainText:
                    while ((text.Length * CHARSIZE) % SOURCEBLOCKSIZE != 0)
                    {
                        text += "\0";
                    }
                    blocksCount = text.Length * CHARSIZE / SOURCEBLOCKSIZE;
                    break;

                case InputMode.Hex:
                    while ((text.Length * CHARSIZE/2) % SOURCEBLOCKSIZE != 0)
                    {
                        text += "00";
                    }
                    blocksCount = text.Length * CHARSIZE/2 / SOURCEBLOCKSIZE;
                    break;
            }

            switch (mode)
            {
                case InputMode.PlainText:
                    for (int i = 0; i < blocksCount; i++)
                    {
                        yield return string.Join("", text.Substring(i * CHARSIZE, CHARSIZE)
                            .Select(x => CharToBinary(x)));
                    }
                    break;

                case InputMode.Hex:
                    for (int i = 0; i < blocksCount; i++)
                    {
                        yield return string.Join("", text.SplitByNChars(2)
                            .Select(x => HexToBinary(x)));
                    }
                    break;
            }
        }

        public MainWindowViewModel()
        {
            IsEncrypt = "none";
            InputText = "0123456789ABCDEF";
            InputKey = "133457799BBCDFF1";
        }

        private Action<object> StartOperation(int type)
        {
            //var text = CompleteAndDivide("0123456789ABCDEF", InputMode.Hex);
            if (string.IsNullOrWhiteSpace(InputText) || string.IsNullOrWhiteSpace(InputKey))
            {
                MessageBox.Show("Text or key for operation cannot be empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            var result = string.Empty;
            TextAndKey = $"Initial text: {InputText}\nKey: {InputKey}";
            InfoEncoded = new ObservableCollection<RoundInfo>();
            InfoDecoded = new ObservableCollection<RoundInfo>();

            GenerateRoundKeys(type);

            switch(type)
            {
                case 0:
                    var text = CompleteAndDivide(InputText, InputMode.Hex);
                    foreach (var block in text)
                    {
                        var encodedBlock = EncodeBlock(block);

                        var encodedPart = string.Join("",
                            encodedBlock.SplitByNChars(8)
                            .Select(x => Convert.ToInt32(x, 2))
                            .Select(x =>
                            {
                                var temp = x.ToString("X");
                                return temp.Length == 2 ? temp : "0" + temp;
                            })
                        );

                        Result = $"Encrypted text: {encodedPart}";
                        ResultField = Result.Split(' ')[2];
                        result += encodedPart;
                    }
                    IsEncrypt = "yes";
                    break;
                case 1:
                    foreach (var block in CompleteAndDivide(InputText, InputMode.Hex))
                    {
                        var decodedBlock = DecodeBlock(block);

                        result += string.Join("",
                            decodedBlock.SplitByNChars(8)
                            .Select(x => Convert.ToInt32(x, 2))
                            .Select(x =>
                            {
                                var temp = x.ToString("X");
                                return temp.Length == 2 ? temp : "0" + temp;
                            })
                        );
                    }
                    IsEncrypt = "no";
                    break;
            }
            return null;
        }

        /// <summary>
        /// Translates a single char into its binary form
        /// </summary>
        /// <param name="char"></param>
        /// <returns></returns>
        private string CharToBinary(char @char)
        {
            var ch = Convert.ToString(@char, 2);

            while (ch.Length < CHARSIZE)
            {
                ch = "0" + ch; // add up to 8 bits if needed
            }

            return ch;
        }

        private string HexToBinary(string hexString)
        {
            return FillBinaryString(Convert.ToString(Convert.ToInt32(hexString, 16), 2));
        }

        private string BinaryToHex(string binString)
        {
            var result = string.Empty;

            for(int i = 0; i < binString.Length; i += 8)
            {
                var temp = Convert.ToString(Convert.ToInt32(binString.Substring(i, 8), 2), 16).ToUpper();
                if (temp.Length < 2) temp = "0" + temp;
                result += temp;
            }

            return result;
        }

        private string FillBinaryString(string source)
        {
            while (source.Length < CHARSIZE)
            {
                source = "0" + source;
            }
            return source;
        }

        private string InitialPermutation(string text)
        {
            return Transpose(TransposeType.InitialPermutation, text, ManagerMatrix.GetInitialPermutationMatrix());
        }

        private string BinaryKey(string keyword, InputMode mode)
        {
            IEnumerable<string> key;

            if(mode == InputMode.Hex)
            {
                key = keyword.SplitByNChars(2).Select(x => HexToBinary(x));
            }
            else
            {
                key = keyword.Select(x => CharToBinary(x));
            }

            return string.Join("", key);
        }

        private string GenerateKey()
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
                    key = key.Substring(key.Length-1) + key.Substring(0, key.Length - 1);
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

            while(result.Length < 4)
            {
                result = "0" + result;
            }

            return result;
        }

        private string SBoxOperation(string expression)
        {
            var result = string.Empty;

            for(int i = 0; i < 8; i++)
            {
                result += ProcessSBox(expression.Substring(i * 6, 6), ManagerMatrix.GetSBoxMatrix(i+1));
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
            InitialPermutationText = InitialPermutation(block);

            LRs[0, 0] = InitialPermutationText.Substring(0, InitialPermutationText.Length / 2);
            LRs[1, 0] = InitialPermutationText.Substring(InitialPermutationText.Length / 2);

            InitialPermutationText = $"Initial permutation: {BinaryToHex(InitialPermutationText)}\n" +
                $"Parts: L0 = {BinaryToHex(LRs[0, 0])}; R0 = {BinaryToHex(LRs[1, 0])}";

            GetLeftRight(InfoEncoded);

            var blockToEncode = LRs[1, 16] + LRs[0, 16];
            AfterJoin = $"After join: {BinaryToHex(blockToEncode)}";

            return Transpose(TransposeType.FinalPermutation, blockToEncode, ManagerMatrix.GetFinalPermutationMatrix());
        }

        private string DecodeBlock(string block)
        {
            var permutatedText = InitialPermutation(block);

            GetLeftRight(InfoDecoded, true);

            var blockToDecode = LRs[0, 0] + LRs[1, 0];

            return Transpose(TransposeType.FinalPermutation, blockToDecode, ManagerMatrix.GetFinalPermutationMatrix());
        }

        private void GetLeftRight(ObservableCollection<RoundInfo> info, bool isReverse = false)
        {
            if(isReverse)
            {
                for (int i = 16; i > 0; i--)
                {
                    LRs[1, i-1] = LRs[0, i];

                    var expandedBlock = Transpose(TransposeType.ExpandedBlock, LRs[0, i], ManagerMatrix.GetExpandedBlockMatrix());
                    var expression = FFunction(expandedBlock, roundKeys[i-1]);

                    LRs[0, i-1] = XOROperation(LRs[1, i], expression);
                }
            }
            else
            {
                for (int i = 1; i < 17; i++)
                {
                    LRs[0, i] = LRs[1, i - 1];

                    var expandedBlock = Transpose(TransposeType.ExpandedBlock, LRs[1, i - 1], ManagerMatrix.GetExpandedBlockMatrix());
                    var expression = FFunction(expandedBlock, roundKeys[i-1]);

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
        }

        private void GenerateRoundKeys(int type)
        {
            var initialKey = BinaryKey(InputKey, InputMode.Hex);

            var key = Transpose(TransposeType.CompressedKey, initialKey, ManagerMatrix.GetCompressedKeyMatrix());

            var keyParts = new string[17, 2];
            keyParts[0, 0] = key.Substring(0, key.Length / 2);
            keyParts[0, 1] = key.Substring(key.Length / 2);

            var oneTimeShift = new[] { 1, 2, 9, 16 };

            for (int i = 1; i < 17; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    keyParts[i, j] = Shift(keyParts[i - 1, j], oneTimeShift.Contains(i) ? 1 : 2, type == 1 ? Direction.Right : Direction.Left);
                }
            }

            for (int i = 0; i < 16; i++)
            {
                roundKeys[i] = Transpose(
                    TransposeType.RoundKey,
                    string.Join("", keyParts[i + 1, 0], keyParts[i + 1, 1]), // +1 coz this this array contains 17 elements
                    ManagerMatrix.GetPBoxCompressMatrix()
                );
            }
        }
    }
}
