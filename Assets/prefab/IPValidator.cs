using UnityEngine;
using TMPro;
[CreateAssetMenu(fileName = "IPValidator", menuName = "TMP Input Validators/IP Validator")]
public class IPValidator : TMP_InputValidator
{
    public override char Validate(ref string text, ref int pos, char ch)
    {
        if (ch >= '0' && ch <= '9' || ch == '.')
        {
            text = text.Insert(pos, ch.ToString());
            pos++;
            return ch;
        }
        Debug.Log("Invalid char: " + ch);
        return '\0';
    }
}
