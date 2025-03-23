using UnityEngine;
using TMPro;

public class VRKeyboardController : MonoBehaviour
{
    private TouchScreenKeyboard keyboard;
    private TMP_InputField tmpInputField;

    void Start()
    {
        tmpInputField = GetComponent<TMP_InputField>();
        tmpInputField.onSelect.AddListener(OpenKeyboard);
    }

    // Otvorí klávesnicu
    void OpenKeyboard(string text)
    {
        if (keyboard == null || !keyboard.active)
        {
            keyboard = TouchScreenKeyboard.Open(text, TouchScreenKeyboardType.Default, false, false, false, false);
        }
    }

    void Update()
    {
        if (keyboard != null && keyboard.active)
        {
            tmpInputField.text = keyboard.text;
        }
    }
}
