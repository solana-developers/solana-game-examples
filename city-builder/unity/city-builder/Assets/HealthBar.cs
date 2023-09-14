using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Image GreenBar;
    public TextMeshProUGUI HealthText;

    public void SetData(int health, int maxHealth)
    {
        GreenBar.transform.localScale = new Vector3(health / (float) maxHealth, 1, 1);
        HealthText.text = $"{health}/{maxHealth}";
    }
}
