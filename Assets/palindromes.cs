using UnityEngine;
using System.Linq;
using System.Collections;
using System.Text.RegularExpressions;

public class Palindromes : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Info;
    public KMBombModule Module;
    public MeshRenderer Component;
    public KMSelectable[] Buttons;
    public TextMesh[] Text;

    bool isSolved = false;
    private bool _isAnimating = false;
    private byte _current = 0;
    static int _moduleIdCounter = 1;
    int _moduleId;
#pragma warning disable 414
    string x = "", y = "", z = "", n = "";
#pragma warning restore 414
    
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
        if (btn < 3)
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[btn].transform);
            Buttons[btn].AddInteractionPunch();
        }

        //if solved, do nothing
        if (isSolved || _isAnimating)
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
            if (x.Length < 9)
            {
                x += _current;
                //if left half has been inputted, generate palindrome
                if (x.Length == 5)
                    for (sbyte i = 3; i >= 0; i--)
                        x += x[i];
            }

            //if y hasn't been filled yet
            else if (y.Length < 8)
            {
                y += _current;
                //if left half has been inputted, generate palindrome
                if (y.Length == 4)
                    for (sbyte i = 3; i >= 0; i--)
                        y += y[i];
            }

            //if z hasn't been filled yet
            else if (z.Length < 7)
            {
                z += _current;
                //if left half has been inputted, generate palindrome
                if (z.Length == 4)
                    for (sbyte i = 2; i >= 0; i--)
                        z += z[i];
            }
            _current = 0;
        }

        //if right button was pressed, delete the current variable
        else if (btn == 2)
        {
            Audio.PlaySoundAtTransform("delete", Buttons[btn].transform);
            if (z.Length != 0)
                z = "";
            else if (y.Length != 0)
                y = "";
            else if (x.Length != 0)
                x = "";
            _current = 0;
        }

        //a fourth button normally doesn't exist on this module, but this is meant for twitchplays to auto-solve with
        else
        {
            x = "000000000";
            y = "00000000";
            z = "0000000";
        }

        //update text on the button
        Text[2].text = _current.ToString();

        //if x is unfinished, display current digit on x
        if (x.Length < 9)
        {
            Text[1].text = "X  =  " + x + _current;
            Text[1].text += "\nY  =  " + y;
            Text[1].text += "\nZ  =  " + z;
        }

        //if y is unfinished, display current digit on y
        else if (y.Length < 8)
        {
            Text[1].text = "X  =  " + x;
            Text[1].text += "\nY  =  " + y + _current;
            Text[1].text += "\nZ  =  " + z;
        }

        //if z is unfinished, display current digit on z
        else if (z.Length < 7)
        {
            Text[1].text = "X  =  " + x;
            Text[1].text += "\nY  =  " + y;
            Text[1].text += "\nZ  =  " + z + _current;
        }

        //if everything has been filled, check if the answer is correct
        else
        {
            Audio.PlaySoundAtTransform("calculate", Buttons[1].transform);
            Debug.LogFormat("[Palindromes #{0}]: Submitting numbers \"{1}\", \"{2}\", and \"{3}\".", _moduleId, x, y, z);
            Text[1].text = "X  =  " + x;
            Text[1].text += "\nY  =  " + y;
            Text[1].text += "\nZ  =  " + z;

            //if x + y + z is are not equal to the screen display, the module should strike, if the nonexistent button is pushed, it's asking for an auto-solve
            bool strike = false;
            if ((int.Parse(x) + int.Parse(y) + int.Parse(z)) % 1000000000 != int.Parse(Text[0].text) && btn < 3)
                strike = true;

            //if anyone of the above parameters are true, strike the module here
            StartCoroutine(Answer(strike));
        }
    }

    private IEnumerator Answer(bool strike)
    {
        n = Text[0].text;
        _isAnimating = true;

        int temp = int.Parse(Text[0].text), total = (int.Parse(x) + int.Parse(y) + int.Parse(z) % 1000000000), inc = 0;
        while (inc < 10000)
        {
            inc += 125;
            float f = inc;
            f /= 10000;
            Text[0].text = Mathf.Clamp(temp - total * BackOut(f), -999999999, 999999999).ToString("#########") + "";
            yield return new WaitForSeconds(0.021f);
        }

        //strike
        if (strike)
        {
            Audio.PlaySoundAtTransform("answer", Buttons[1].transform);
            Debug.LogFormat("[Palindromes #{0}]: Strike! Variable X, Y, and Z does not add up to the screen display!", _moduleId);
            Module.HandleStrike();

            Text[1].text = "";
            string error = "ERROR:\nX+Y+Z does\nnot equal N!";
            
            for (byte i = 0; i < error.Length; i++)
            {
                Text[1].text += error[i];
                yield return new WaitForSeconds(0.021f);
            }

            float f = 0;
            while (f < 1)
            {
                Text[0].color = new Color32((byte)(Text[0].color.r * 255), (byte)(Text[0].color.g * 255), (byte)(Text[0].color.b * 255), (byte)((1 - CubicOut(f)) * 255));
                Text[1].color = new Color32((byte)(Text[0].color.r * 255), (byte)(Text[0].color.g * 255), (byte)(Text[0].color.b * 255), (byte)((1 - CubicOut(f)) * 255));
                f += 0.0125f;
                yield return new WaitForSeconds(0.021f);
            }

            x = "";
            y = "";
            z = "";

            Text[1].text = "X  =  " + _current;
            Text[1].text += "\nY  =  ";
            Text[1].text += "\nZ  =  ";

            Text[0].text = temp.ToString("#########");

            f = 0;
            while (f < 1)
            {
                Text[0].color = new Color32((byte)(Text[0].color.r * 255), (byte)(Text[0].color.g * 255), (byte)(Text[0].color.b * 255), (byte)(CubicOut(f) * 255));
                Text[1].color = new Color32((byte)(Text[0].color.r * 255), (byte)(Text[0].color.g * 255), (byte)(Text[0].color.b * 255), (byte)(CubicOut(f) * 255));
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
            isSolved = true;
            Module.HandlePass();
        }

        _isAnimating = false;
    }

    private static float CubicOut(float k)
    {
        return 1f + ((k -= 1f) * k * k);
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
                while (x.Length != 0)
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
        Debug.LogFormat("[Palindromes #{0}]: Admin has initiated a forced solve... Since generating an answer would take hundreds of algorithms and 40 pages of mathematical manuals to learn, I have decided to just solve instantly!", _moduleId);
        HandlePress(3);
    }
}