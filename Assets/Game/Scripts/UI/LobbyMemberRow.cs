using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMemberRow : MonoBehaviour
{
    [SerializeField] private RawImage avatarImage;
    [SerializeField] private TMP_Text nameText;

    public void SetAvatar(Texture2D texture)
    {
        if (texture == null)
            return;

        avatarImage.texture = texture;

        avatarImage.uvRect = new Rect(0, 1, 1, -1); //flip vertically
    }

    public void SetName(string name)
    {
        nameText.text = name;
    }

}
