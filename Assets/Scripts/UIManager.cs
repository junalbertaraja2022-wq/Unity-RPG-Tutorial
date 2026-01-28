using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Main UI")]
    public GameObject characterInfoPanel;
    public GameObject actionMenu;
    public GameObject battleForecastPanel;
    public GameObject phaseDisplay;
    
    [Header("Character Info")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI classText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI strText;
    public TextMeshProUGUI magText;
    public TextMeshProUGUI sklText;
    public TextMeshProUGUI spdText;
    public TextMeshProUGUI defText;
    public TextMeshProUGUI resText;
    public Image portraitImage;
    
    [Header("Battle Forecast")]
    public TextMeshProUGUI attackerName;
    public TextMeshProUGUI defenderName;
    public TextMeshProUGUI attackerDamage;
    public TextMeshProUGUI defenderDamage;
    public TextMeshProUGUI attackerHit;
    public TextMeshProUGUI defenderHit;
    public TextMeshProUGUI attackerCrit;
    public TextMeshProUGUI defenderCrit;
    
    [Header("Turn Info")]
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI phaseText;
    
    [Header("Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;
    public Button restartButton;
    public Button quitButton;

    [Header("Weapon Info")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI weaponStatsText;
    public Image weaponIconImage;
    public Slider weaponDurabilitySlider;
    
    void Start()
    {
        InitializeUI();
    }
    
    public void InitializeUI()
    {
        // Hide panels initially
        characterInfoPanel.SetActive(false);
        actionMenu.SetActive(false);
        battleForecastPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        
        // Set up buttons
        if (restartButton != null)
            restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
        
        if (quitButton != null)
            quitButton.onClick.AddListener(() => GameManager.Instance.QuitGame());
    }

    public void UpdateCharacterInfo(Character character)
    {
        if (character == null) return;

        characterInfoPanel.SetActive(true);

        characterNameText.text = character.characterName;
        classText.text = character.characterClass.ToString();
        levelText.text = $"Lv. {character.level}";
        hpText.text = $"HP: {character.currentHP}/{character.baseStats.maxHP}";
        strText.text = $"Str: {character.baseStats.strength}";
        magText.text = $"Mag: {character.baseStats.magic}";
        sklText.text = $"Skl: {character.baseStats.skill}";
        spdText.text = $"Spd: {character.baseStats.speed}";
        defText.text = $"Def: {character.baseStats.defense}";
        resText.text = $"Res: {character.baseStats.resistance}";

        if (portraitImage != null && character.portrait != null)
        {
            portraitImage.sprite = character.portrait;
        }

        // Update equipped weapon
        // ... (add weapon info display)
    }
    
    public void UpdateWeaponInfo(Character character)
    {
        if (character.equippedWeapon != null)
        {
            WeaponData weapon = character.equippedWeapon;
            
            weaponNameText.text = weapon.weaponName;
            weaponStatsText.text = $"Mt:{weapon.might} Hit:{weapon.hit}% Crt:{weapon.crit}% Rng:{weapon.range}";
            
            if (weaponIconImage != null && weapon.icon != null)
            {
                weaponIconImage.sprite = weapon.icon;
            }
            
            if (weaponDurabilitySlider != null)
            {
                weaponDurabilitySlider.value = weapon.GetDurabilityPercent();
            }
        }
    }
    
    public void ShowActionMenu(Vector2 screenPosition)
    {
        actionMenu.SetActive(true);
        actionMenu.transform.position = screenPosition;
    }
    
    public void HideActionMenu()
    {
        actionMenu.SetActive(false);
    }
    
    public void ShowBattleForecast(Character attacker, Character defender)
    {
        battleForecastPanel.SetActive(true);
        
        attackerName.text = attacker.characterName;
        defenderName.text = defender.characterName;
        
        // Calculate battle preview
        int attackerDamage = attacker.CalculateDamage(defender);
        int defenderDamage = defender.CalculateDamage(attacker);
        
        this.attackerDamage.text = $"DMG: {attackerDamage}";
        this.defenderDamage.text = $"DMG: {defenderDamage}";
        
        // Calculate hit rates (simplified)
        this.attackerHit.text = "HIT: 80%";
        this.defenderHit.text = "HIT: 70%";
        
        // Calculate crit rates (simplified)
        this.attackerCrit.text = "CRT: 5%";
        this.defenderCrit.text = "CRT: 3%";
    }
    
    public void HideBattleForecast()
    {
        battleForecastPanel.SetActive(false);
    }
    
    public void ShowPhaseText(string phase)
    {
        if (phaseText != null)
        {
            phaseText.text = phase;
            
            // Show briefly then fade
            StartCoroutine(FadePhaseText());
        }
    }
    
    System.Collections.IEnumerator FadePhaseText()
    {
        phaseText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        
        float fadeDuration = 1f;
        float elapsed = 0f;
        Color startColor = phaseText.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0);
        
        while (elapsed < fadeDuration)
        {
            phaseText.color = Color.Lerp(startColor, endColor, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        phaseText.gameObject.SetActive(false);
        phaseText.color = startColor;
    }
    
    public void ShowBattleAnimation(Character attacker, Character defender, BattleResult result)
    {
        // Create battle animation
        StartCoroutine(PlayBattleAnimation(attacker, defender, result));
    }
    
    System.Collections.IEnumerator PlayBattleAnimation(Character attacker, Character defender, BattleResult result)
    {
        // Move characters towards each other
        Vector3 attackerStart = attacker.transform.position;
        Vector3 defenderStart = defender.transform.position;
        Vector3 battleCenter = (attackerStart + defenderStart) / 2;
        
        // Attacker attacks
        yield return StartCoroutine(MoveToBattlePosition(attacker, battleCenter));
        yield return new WaitForSeconds(0.3f);
        
        // Show damage numbers
        if (result.damageToDefender > 0)
        {
            ShowDamageNumber(defender.transform.position, result.damageToDefender, result.attackerCritical);
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Return to position
        yield return StartCoroutine(ReturnToPosition(attacker, attackerStart));
        
        // Defender counterattacks if applicable
        if (result.defenderCountered)
        {
            yield return new WaitForSeconds(0.3f);
            
            yield return StartCoroutine(MoveToBattlePosition(defender, battleCenter));
            yield return new WaitForSeconds(0.3f);
            
            if (result.damageToAttacker > 0)
            {
                ShowDamageNumber(attacker.transform.position, result.damageToAttacker, result.defenderCritical);
            }
            
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(ReturnToPosition(defender, defenderStart));
        }
    }
    
    System.Collections.IEnumerator MoveToBattlePosition(Character character, Vector3 target)
    {
        Vector3 start = character.transform.position;
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            character.transform.position = Vector3.Lerp(start, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        character.transform.position = target;
    }
    
    System.Collections.IEnumerator ReturnToPosition(Character character, Vector3 target)
    {
        Vector3 start = character.transform.position;
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            character.transform.position = Vector3.Lerp(start, target, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        character.transform.position = target;
    }
    
    void ShowDamageNumber(Vector3 position, int damage, bool isCritical)
    {
        GameObject damageText = new GameObject("DamageText");
        damageText.transform.position = position + Vector3.up;
        
        TextMeshPro textMesh = damageText.AddComponent<TextMeshPro>();
        textMesh.text = damage.ToString();
        textMesh.fontSize = isCritical ? 6 : 4;
        textMesh.color = isCritical ? Color.yellow : Color.red;
        textMesh.alignment = TextAlignmentOptions.Center;
        
        // Animate
        StartCoroutine(AnimateDamageText(damageText));
    }
    
    System.Collections.IEnumerator AnimateDamageText(GameObject textObject)
    {
        float duration = 1f;
        float elapsed = 0f;
        Vector3 startPos = textObject.transform.position;
        
        while (elapsed < duration)
        {
            textObject.transform.position = startPos + Vector3.up * (elapsed * 2f);
            
            // Fade out
            TextMeshPro textMesh = textObject.GetComponent<TextMeshPro>();
            Color color = textMesh.color;
            color.a = 1 - (elapsed / duration);
            textMesh.color = color;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        Destroy(textObject);
    }
    
    public void ShowGameOverScreen(bool victory)
    {
        gameOverPanel.SetActive(true);
        
        if (gameOverText != null)
        {
            gameOverText.text = victory ? "Victory!" : "Defeat...";
            gameOverText.color = victory ? Color.green : Color.red;
        }
    }
    
    public void UpdateTurnInfo(int turn, GamePhase phase)
    {
        if (turnText != null)
        {
            turnText.text = $"Turn {turn}";
        }
    }
}