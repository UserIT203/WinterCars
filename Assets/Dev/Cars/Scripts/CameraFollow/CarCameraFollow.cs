using UnityEngine;

// Камера следит за машиной игрока. Цель ищется сама и меняется,
// когда игрок пересаживается в другую машину
[DisallowMultipleComponent]
public class CarCameraFollow : MonoBehaviour
{
    [Header("Target"), Space(5f)]
    [Tooltip("Запасная цель на случай, когда машины игрока на сцене нет. " +
        "Пока игрок за рулём, камера следит за его машиной и это поле игнорируется")]
    [SerializeField] private Transform _fallbackTarget;

    [Header("Offset"), Space(5f)]
    [Tooltip("Смещение камеры относительно машины: X — вбок, Y — вверх, Z — назад (отрицательный)")]
    [SerializeField] private Vector3 _offset = new Vector3(0f, 4f, -8f);

    [Tooltip("Куда смотрит камера относительно машины: точка прицела над её центром")]
    [SerializeField] private Vector3 _lookOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Smoothing"), Space(5f)]
    [Tooltip("За сколько секунд камера догоняет позицию. 0 — жёстко без сглаживания")]
    [SerializeField] private float _positionSmoothTime = 0.15f;

    [Tooltip("Скорость доворота камеры на машину, градусов в секунду")]
    [SerializeField] private float _rotationSpeed = 720f;

    [Tooltip("Смещение считать в осях машины (камера едет за её хвостом), а не в мировых")]
    [SerializeField] private bool _followRotation = true;

    private Vector3 _positionVelocity;

    private void LateUpdate()
    {
        Transform target = ResolveTarget();
        if(target == null) return;

        Vector3 desiredPosition = target.position + (_followRotation
            ? target.rotation * _offset
            : _offset);

        transform.position = _positionSmoothTime > 0f
            ? Vector3.SmoothDamp(transform.position, desiredPosition, ref _positionVelocity, _positionSmoothTime)
            : desiredPosition;

        Vector3 lookPoint = target.position + target.rotation * _lookOffset;
        Vector3 direction = lookPoint - transform.position;

        // На нулевом расстоянии направление вырождается — поворот в этот кадр пропускаем
        if(direction.sqrMagnitude < 0.0001f) return;

        Quaternion desiredRotation = Quaternion.LookRotation(direction, Vector3.up);

        transform.rotation = _rotationSpeed > 0f
            ? Quaternion.RotateTowards(transform.rotation, desiredRotation, _rotationSpeed * Time.deltaTime)
            : desiredRotation;
    }

    private Transform ResolveTarget()
    {
        Car playerCar = Car.PlayerCar;
        if(playerCar != null) return playerCar.transform;

        return _fallbackTarget;
    }

    public void SnapToTarget()
    {
        Transform target = ResolveTarget();
        if(target == null) return;

        _positionVelocity = Vector3.zero;

        transform.position = target.position + (_followRotation
            ? target.rotation * _offset
            : _offset);

        transform.LookAt(target.position + target.rotation * _lookOffset, Vector3.up);
    }
}
