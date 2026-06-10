using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public enum Weapon { Pistol, Knife }

    [Header("UI Reference (drag iz Hierarchyja)")]
    public Image healthRing;
    public Image pistolIcon; // koristi se i za pistolj i za noz (samo ime nije precizno)
    public TMP_Text ammoText;

    [Header("Sprite-ovi za pistolj")]
    public Sprite pistolFullSprite;
    public Sprite pistolEmptySprite;

    [Header("Sprite-ovi za noz")]
    public Sprite knifeSprite;
    public Sprite knifeBrokenSprite; // za buduce, ako bude trebalo

    [Header("Postavke")]
    public int maxHealth = 100;
    public int maxAmmo = 15;

    [Header("Trenutno stanje")]
    public int currentHealth = 100;
    public int currentAmmo = 15;
    public Weapon currentWeapon = Weapon.Pistol;

    void Start()
    {
        RefreshHUD();
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        RefreshHUD();
    }

    public void UseBullet()
    {
        if (currentWeapon != Weapon.Pistol) return; // metak troši samo pištolj
        currentAmmo = Mathf.Clamp(currentAmmo - 1, 0, maxAmmo);
        RefreshHUD();
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        RefreshHUD();
    }

    public void RefillAmmo()
    {
        currentAmmo = maxAmmo;
        RefreshHUD();
    }

    // Postavi stanje municije izvana (npr. iz DR_PlayerShooter)
    public void SetAmmo(int current, int max)
    {
        maxAmmo = Mathf.Max(0, max);
        currentAmmo = Mathf.Clamp(current, 0, maxAmmo);
        RefreshHUD();
    }

    public void SwitchToPistol()
    {
        currentWeapon = Weapon.Pistol;
        RefreshHUD();
    }

    public void SwitchToKnife()
    {
        currentWeapon = Weapon.Knife;
        RefreshHUD();
    }

    // Korisno za skripte koje samo žele toggleat oružje na pritisak tipke
    public void ToggleWeapon()
    {
        currentWeapon = (currentWeapon == Weapon.Pistol) ? Weapon.Knife : Weapon.Pistol;
        RefreshHUD();
    }

 void RefreshHUD()
{
    // Health ring fill - remap u [MIN_FILL, MAX_FILL] da odgovara C-spriteu
    if (healthRing != null)
    {
        const float MIN_FILL = 0f;
        const float MAX_FILL = 0.997f;
        float healthPercent = (float)currentHealth / maxHealth;
        healthRing.fillAmount = MIN_FILL + healthPercent * (MAX_FILL - MIN_FILL);
    }

    // Ikona i tekst ovise o trenutnom oruzju
    if (currentWeapon == Weapon.Pistol)
    {
        if (pistolIcon != null && pistolFullSprite != null && pistolEmptySprite != null)
            pistolIcon.sprite = (currentAmmo > 0) ? pistolFullSprite : pistolEmptySprite;

        if (ammoText != null)
            ammoText.text = currentAmmo + "/" + maxAmmo;
    }
    else // Knife
    {
        if (pistolIcon != null && knifeSprite != null)
            pistolIcon.sprite = knifeSprite;

        if (ammoText != null)
            ammoText.text = "";
    }
}
    // ==== Test gumbi (desni klik na skriptu u Inspectoru) ====

    [ContextMenu("TEST: Izgubi 10 HP")]
    void TestDamage() { TakeDamage(10); }

    [ContextMenu("TEST: Vrati pun HP")]
    void TestResetHealth() { Heal(maxHealth); }

    [ContextMenu("TEST: Ispali metak")]
    void TestShoot() { UseBullet(); }

    [ContextMenu("TEST: Napuni metke")]
    void TestRefill() { RefillAmmo(); }

    [ContextMenu("TEST: Isprazni metke")]
    void TestEmpty() { currentAmmo = 0; RefreshHUD(); }

    [ContextMenu("TEST: Switch na pistolj")]
    void TestSwitchPistol() { SwitchToPistol(); }

    [ContextMenu("TEST: Switch na noz")]
    void TestSwitchKnife() { SwitchToKnife(); }

    [ContextMenu("TEST: Toggle oruzje")]
    void TestToggle() { ToggleWeapon(); }
}