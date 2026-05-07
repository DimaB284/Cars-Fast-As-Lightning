using UnityEngine;

public class QTEZone : MonoBehaviour
{
    public enum QTEType { Boost, Obstacle }
    public QTEType type = QTEType.Boost;
    
    [Header("Settings")]
    public float timeToReact = 1.0f; // Скільки секунд є у гравця
    public float boostMultiplier = 1.5f; // Наскільки сильно прискорить "Perfect"

    private void OnTriggerEnter(Collider other)
    {
        // Перевіряємо, чи це наша тачка
        var car = other.GetComponentInParent<CarController>();
        if (car != null)
        {
            // Передаємо дані про івент у контролер тачки
            car.StartQTE(this);
        }
    }
}