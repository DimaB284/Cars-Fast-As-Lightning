using UnityEngine;
using TMPro; // Підключаємо TextMeshPro для красивого тексту
using System.Collections;

public class RaceManager : MonoBehaviour
{
    [Header("Dependencies")]
    public CarController playerCar;
    public TextMeshProUGUI countdownText; // UI Текст на екрані

    [Header("Start Boost Settings")]
    public float perfectStartWindow = 0.4f; // Скільки секунд є у гравця на натискання "GO!"
    public float startBoostMultiplier = 2f; // У скільки разів прискоримо тачку
    public float boostDuration = 1.5f;

    private void Start()
    {
        // На старті гри блокуємо рух машинки
        if (playerCar != null) playerCar.canMove = false;
        
        // Запускаємо корутину відліку
        StartCoroutine(StartRaceSequence());
    }

    private IEnumerator StartRaceSequence()
    {
        countdownText.gameObject.SetActive(true);

        countdownText.text = "3";
        yield return new WaitForSeconds(1f);

        countdownText.text = "2";
        yield return new WaitForSeconds(1f);

        countdownText.text = "1";
        yield return new WaitForSeconds(1f);

        // --- МОМЕНТ СТАРТУ ---
        countdownText.text = "GO!";
        if (playerCar != null) playerCar.canMove = true;

        float timer = 0f;
        bool boostAwarded = false;

        // Даємо гравцю невеличке "вікно" часу (наприклад, 0.4 сек), щоб натиснути педаль
        while (timer < perfectStartWindow)
        {
            timer += Time.deltaTime;
            
            if (playerCar.isPedalPressed && !boostAwarded)
            {
                Debug.Log("<color=orange>PERFECT START BOOST!</color>");
                playerCar.ApplyTemporaryBoost(startBoostMultiplier, boostDuration);
                boostAwarded = true;
            }
            yield return null; // Чекаємо наступного кадру
        }

        // Чекаємо залишок секунди, щоб напис "GO!" повисів на екрані, і ховаємо його
        yield return new WaitForSeconds(1f - timer); 
        countdownText.gameObject.SetActive(false);
    }
}