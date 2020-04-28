using UnityEngine;
using System.Linq;
using System.Collections;
using System.Text.RegularExpressions;

public class palindromes : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Info;
    public KMBombModule Module;
    public MeshRenderer Component;
    public KMSelectable[] Buttons;
    public TextMesh[] Text;

    bool _isSolved = false;
    private bool _isAnimating = false;
    private byte _current = 0;
    static int _moduleIdCounter;
    int _moduleId;
    string _x = "", _y = "", _z = "";

    private void Awake()
    {
        _moduleId = _moduleIdCounter++;
        bool palindrome = true;
        ushort attempts = 0;
        //makes sure that it doesn't generate a palindrome to prevent very rare unicorns
        if (!Application.isEditor)
            do
            {
                attempts++;
                palindrome = true;
                //generates a 9-digit number for the screen display
                Text[0].text = Random.Range(1, 1000000000).ToString();
                Debug.LogFormat("[Palindromes #{0}]: Generated number \"{1}\"", _moduleId, Text[0].text);

                //anti-palindrome check
                for (byte i = 0; i < Text[0].text.Length; i++)
                    if (Text[0].text[i] != Text[0].text[Text[0].text.Length - i - 1])
                    {
                        palindrome = false;
                        break;
                    }

                if (Application.isEditor)
                    palindrome = palindrome && Text[0].text.Length == 9;
            } while (palindrome ^ Application.isEditor);
        //palindrome generator for debug so that it doesn't take me 2 years to solve the darn thing
        else
        {
            attempts++;
            Text[0].text = Random.Range(10000, 100000).ToString();
            for (sbyte i = 3; i >= 0; i--)
                Text[0].text += Text[0].text[i];
        }

        //debugging individual numbers
        //Text[0].text = "123456789";

        while (Text[0].text.Length < 9)
            Text[0].text = Text[0].text.Insert(0, "0");

        Debug.LogFormat("[Palindromes #{0}]: Recieved screen \"{1}\", taking {2} attempt(s).", _moduleId, Text[0].text, attempts);

        //puts in correct index when you push one of the three buttons
        for (byte i = 0; i < Buttons.Length; i++)
        {
            byte j = i;

            Buttons[i].OnInteract += delegate ()
            {
                HandlePress(j);
                return false;
            };
        }
    }

    private void HandlePress(byte btn)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[btn].transform);
        Buttons[btn].AddInteractionPunch();

        //if solved, do nothing
        if (_isSolved || _isAnimating)
            return;

        //if left button was pressed, cycle from 0 through 9
        if (btn == 0)
        {
            Audio.PlaySoundAtTransform("cycle", Buttons[btn].transform);
            _current = (byte)(++_current % 10);
        }

        //if middle button was pressed, add it to the list
        else if (btn == 1)
        {
            Audio.PlaySoundAtTransform("submit", Buttons[btn].transform);

            //if x hasn't been filled yet
            if (_x.Length < 9)
            {
                _x += _current;
                //if left half has been inputted, generate palindrome
                if (_x.Length == 5)
                    for (sbyte i = 3; i >= 0; i--)
                        _x += _x[i];
            }

            //if y hasn't been filled yet
            else if (_y.Length < 8)
            {
                _y += _current;
                //if left half has been inputted, generate palindrome
                if (_y.Length == 4)
                    for (sbyte i = 3; i >= 0; i--)
                        _y += _y[i];
            }

            //if z hasn't been filled yet
            else if (_z.Length < 7)
            {
                _z += _current;
                //if left half has been inputted, generate palindrome
                if (_z.Length == 4)
                    for (sbyte i = 2; i >= 0; i--)
                        _z += _z[i];
            }
            _current = 0;
        }

        //if right button was pressed, delete the current variable
        else
        {
            Audio.PlaySoundAtTransform("delete", Buttons[btn].transform);
            if (_z.Length != 0)
                _z = "";
            else if (_y.Length != 0)
                _y = "";
            else if (_x.Length != 0)
                _x = "";
            _current = 0;
        }

        //update text on the button
        Text[2].text = _current.ToString();

        //if x is unfinished, display current digit on x
        if (_x.Length < 9)
        {
            Text[1].text = "X  =  " + _x + _current;
            Text[1].text += "\nY  =  " + _y;
            Text[1].text += "\nZ  =  " + _z;
        }

        //if y is unfinished, display current digit on y
        else if (_y.Length < 8)
        {
            Text[1].text = "X  =  " + _x;
            Text[1].text += "\nY  =  " + _y + _current;
            Text[1].text += "\nZ  =  " + _z;
        }

        //if z is unfinished, display current digit on z
        else if (_z.Length < 7)
        {
            Text[1].text = "X  =  " + _x;
            Text[1].text += "\nY  =  " + _y;
            Text[1].text += "\nZ  =  " + _z + _current;
        }

        //if everything has been filled, check if the answer is correct
        else
        {
            Audio.PlaySoundAtTransform("calculate", Buttons[btn].transform);
            Debug.LogFormat("[Palindromes #{0}]: Submitting numbers \"{1}\", \"{2}\", and \"{3}\".", _moduleId, _x, _y, _z);
            Text[1].text = "X  =  " + _x;
            Text[1].text += "\nY  =  " + _y;
            Text[1].text += "\nZ  =  " + _z;

            byte strike = 0;

            //if x isn't a palindrome, the module should strike
            for (byte i = 0; i < _x.Length && strike == 0; i++)
                if (_x[i] != _x[_x.Length - i - 1])
                {
                    strike = 1;
                    Debug.LogFormat("[Palindromes #{0}]: Variable X (\"{1}\") is not palindromic!", _moduleId, _x);
                }

            //if y isn't a palindrome, the module should strike
            for (byte i = 0; i < _y.Length && strike == 0; i++)
                if (_y[i] != _y[_y.Length - i - 1])
                { 
                    strike = 2;
                    Debug.LogFormat("[Palindromes #{0}]: Variable Y (\"{1}\") is not palindromic!", _moduleId, _y);
                }

            //if z isn't a palindrome, the module should strike
            for (byte i = 0; i < _z.Length && strike == 0; i++)
                if (_z[i] != _z[_z.Length - i - 1])
                {
                    strike = 3;
                    Debug.LogFormat("[Palindromes #{0}]: Variable Z (\"{1}\") is not palindromic!", _moduleId, _z);
                }

            //if x + y + z is are not equal to the screen display, the module should strike
            if ((int.Parse(_x) + int.Parse(_y) + int.Parse(_z)) % 1000000000 != int.Parse(Text[0].text) && strike == 0)
            { 
                strike = 4;
                Debug.LogFormat("[Palindromes #{0}]: Variable X, Y, and Z does not add up to the screen display!", _moduleId, _x, _y, _z, Text[0].text);
            }

            //if anyone of the above parameters are true, strike the module here
            StartCoroutine(Answer(strike));
        }
    }

    private IEnumerator Answer(byte strike)
    {
        _isAnimating = true;

        yield return null;

        int temp = int.Parse(Text[0].text), total = (int.Parse(_x) + int.Parse(_y) + int.Parse(_z) % 1000000000), n = 0;
        while (n < 10000)
        {
            n += 125;
            float f = n;
            f /= 10000;
            Text[0].text = Mathf.Clamp(temp - total * BackOut(f), -999999999, 999999999).ToString("#########") + "";
            yield return new WaitForSeconds(0.021f);
        }

        //strike
        if (strike != 0)
        {
            Audio.PlaySoundAtTransform("answer", Buttons[1].transform);
            Debug.LogFormat("[Palindromes #{0}]: Strike! The numbers didn't meet the required parameters.", _moduleId);
            Module.HandleStrike();

            Text[1].text = " ";
            string[] text = { "ERROR-1:\nXis not\npalindromic!", "ERROR-2:\nY is not\npalindromic!", "ERROR-3:\nZ is not\npalindromic!", "ERROR-4:\nX+Y+Z does\nnot equal N!" };
            for (byte i = 0; i < text[strike - 1].Length; i++)
            {
                Text[1].text += text[strike - 1][i];
                yield return new WaitForSeconds(0.021f);
            }

            float f = 0;
            while (f < 1)
            {
                Text[0].color = new Color32((byte)(Text[0].color.r * 255), (byte)(Text[0].color.g * 255), (byte)(Text[0].color.b * 255), (byte)((1 - ExponentialIn(f)) * 255));
                Text[1].color = new Color32((byte)(Text[0].color.r * 255), (byte)(Text[0].color.g * 255), (byte)(Text[0].color.b * 255), (byte)((1 - ExponentialIn(f)) * 255));
                f += 0.0125f;
                yield return new WaitForSeconds(0.021f);
            }

            _x = "";
            _y = "";
            _z = "";

            Text[1].text = "X  =  " + _current;
            Text[1].text += "\nY  =  ";
            Text[1].text += "\nZ  =  ";

            Text[0].text = temp.ToString("#########");

            f = 0;
            while (f < 1)
            {
                Text[0].color = new Color32((byte)(Text[0].color.r * 255), (byte)(Text[0].color.g * 255), (byte)(Text[0].color.b * 255), (byte)(ExponentialIn(f) * 255));
                Text[1].color = new Color32((byte)(Text[0].color.r * 255), (byte)(Text[0].color.g * 255), (byte)(Text[0].color.b * 255), (byte)(ExponentialIn(f) * 255));
                f += 0.0125f;
                yield return new WaitForSeconds(0.011f);
            }
        }
        //solve
        else
        {
            Audio.PlaySoundAtTransform("answer", Buttons[1].transform);
            Text[0].text = "";
            Text[1].text = "YOU  FOUND  IT!";
            Debug.LogFormat("[Palindromes #{0}]: All numbers are palindromic and add up to the screen number, module solved!", _moduleId);
            _isSolved = true;
            Module.HandlePass();
        }

        _isAnimating = false;
    }

    private static float ExponentialIn(float k)
    {
        return k == 0f ? 0f : Mathf.Pow(1024f, k - 1f);
    }

    private static float BackOut(float k)
    {
        float s = 1.70158f;
        return (k -= 1f) * k * ((s + 1f) * k + s) + 1f;
    }

    private bool IsValid(string par)
    {
        //palindrome check
        for (byte i = 0; i < par.Length; i++)
            if (par[i] != par[par.Length - i - 1])
                return false;

        //positive number check
        uint num;
        return uint.TryParse(par, out num);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} submit <#########> <########> <#######> (Submits the numbers on the module. All numbers must be palindromic. Example: !{0} submit 420696024 13377331 0000000)";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] buttonPressed = command.Split(' ');

        //if command is formatted correctly
        if (Regex.IsMatch(buttonPressed[0], @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;

            //if three numbers haven't been submitted
            if (buttonPressed.Length < 4)
                yield return "sendtochaterror Please specify the three numbers you wish to submit.";

            //if more than three numbers have been submitted
            else if (buttonPressed.Length > 4)
                yield return "sendtochaterror Too many numbers submitted! Please submit exactly 3 numbers.";

            //if any input aren't numbers, or aren't palindromes
            else if (!IsValid(buttonPressed[1]) || !IsValid(buttonPressed[2]) || !IsValid(buttonPressed[3]))
                yield return "sendtochaterror The numbers have to be palindromes! (Written the same forwards as backwards.)";

            //if any numbers aren't the correct digit length
            else if (buttonPressed[1].Length != 9 || buttonPressed[2].Length != 8 || buttonPressed[3].Length != 7)
                yield return "sendtochaterror The numbers have to be 9 digits, 8 digits, then 7 digits, in that order.";

            else
            {
                //deletes everything in case if anything was inputted
                while (_x.Length != 0)
                {
                    Buttons[2].OnInteract();
                    yield return new WaitForSeconds(0.15f);
                }

                for (byte i = 1; i <= 3; i++)
                    for (byte j = 0; j < 5; j++)
                    {
                        //the only 5-digit number that needs to be inputted is X
                        if (i != 1 && j == 4)
                            continue;

                        //if the current button isn't equal to what the user submitted, press the left button to cycle through the numbers until it's false
                        while (_current.ToString().ToCharArray()[0] != buttonPressed[i][j])
                        {
                            Buttons[0].OnInteract();
                            yield return new WaitForSeconds(0.05f);
                        }

                        //press the middle button which submits the digit
                        Buttons[1].OnInteract();
                        yield return new WaitForSeconds(0.15f);
                    }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        //autosolve
        yield return null;
        _isSolved = true;
        Debug.LogFormat("[Palindromes #{0}]: Admin has initiated a forced solve... Since generating an answer would take hundreds of algorithms and 40 pages of mathematical manuals to learn, I have decided to just solve instantly!", _moduleId);
        Module.HandlePass();
        Audio.PlaySoundAtTransform("solve", Buttons[1].transform);
    }
}