using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : ICarMovement
{
    // Темп поворота руля, градусов в секунду
    private const float RotationSpeed = 120f;

    // Задний ход медленнее переднего
    private const float ReverseSpeedFactor = 0.4f;

    // Ниже этой скорости руль не работает: на месте машина не крутится
    private const float MinSteeringSpeed = 0.5f;

    public float Speed { get; private set; }

    public float MaxSpeed { get; private set; }

    public float Acceleration { get; private set; }

    public Transform CarTransform { get; private set; }

    public PlayerMovement
        (
            float maxSpeed,
            float acceleration,
            Transform carTrasnform
        )
    {
        MaxSpeed = maxSpeed;
        Acceleration = acceleration;
        CarTransform = carTrasnform;
    }

    public void Move()
    {
        if(CarTransform == null) return;

        Keyboard keyboard = Keyboard.current;
        if(keyboard == null) return;

        float throttle = 0f;
        if(keyboard.wKey.isPressed) throttle += 1f;
        if(keyboard.sKey.isPressed) throttle -= 1f;

        float steering = 0f;
        if(keyboard.dKey.isPressed) steering += 1f;
        if(keyboard.aKey.isPressed) steering -= 1f;

        // Газ тянет скорость к пределу, отпущенный газ — к нулю: машина катится накатом
        float targetSpeed = 0f;
        if(throttle > 0f) targetSpeed = MaxSpeed;
        if(throttle < 0f) targetSpeed = -MaxSpeed * ReverseSpeedFactor;

        Speed = Mathf.MoveTowards(Speed, targetSpeed, Acceleration * Time.deltaTime);

        // Руль поворачивает машину, а не траекторию: стоя на месте, повернуть нельзя.
        // На заднем ходу руль отрабатывает зеркально, как у настоящей машины
        if(Mathf.Abs(Speed) > MinSteeringSpeed)
        {
            float steeringGrip = Mathf.Clamp01(Mathf.Abs(Speed) / MaxSpeed);
            float angle = steering * RotationSpeed * steeringGrip * Mathf.Sign(Speed) * Time.deltaTime;

            CarTransform.Rotate(0f, angle, 0f, Space.World);
        }

        CarTransform.position += CarTransform.forward * Speed * Time.deltaTime;
    }
}
