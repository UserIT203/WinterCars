using UnityEngine;

public class MovementAI : ICarMovement
{
    // Темп доворота, градусов в секунду
    private const float RotationSpeed = 200f;

    // Темп доворота на месте при развороте в тупике
    private const float TurnaroundRotationSpeed = 60f;

    // Цель считается позади, если косинус угла меньше этого значения
    private const float BehindThreshold = -0.3f;

    private RoadNode _currentNode;
    private RoadNode _previousNode;
    private float _arriveDistance;

    public float Speed { get; private set; }

    public float MaxSpeed { get; private set; }

    public float Acceleration { get; private set; }

    public Transform CarTransform { get; private set; }

    public MovementAI
        (
        float maxSpeed,
        float acceleration,
        RoadNode startNode,
        float arriveDistance,
        Transform carTransform
        )
    {
        MaxSpeed = maxSpeed;
        Acceleration = acceleration;

        _currentNode = startNode;
        _arriveDistance = arriveDistance;
        CarTransform = carTransform;
    }

    public void Move()
    {
        if (_currentNode == null) return;

        Vector3 distanceToTarget = _currentNode.Position - CarTransform.position;
        distanceToTarget.y = 0f;

        for(int guard = 0; guard < 4 && distanceToTarget.magnitude <= _arriveDistance; guard++)
        {
            RoadNode next = _currentNode.GetNext(CarTransform.forward);

            // Глухой тупик: возвращаемся тем же путём, каким приехали
            if(next == null) next = _previousNode;
            if(next == null) return;

            _previousNode = _currentNode;
            _currentNode = next;

            distanceToTarget = _currentNode.Position - CarTransform.position;
            distanceToTarget.y = 0f;
        }

        if(distanceToTarget.sqrMagnitude < 0.0001f) return;

        Quaternion lookAtTarget = Quaternion.LookRotation(distanceToTarget);

        // Цель позади: тормозим и доворачиваемся на месте, пока не встанем к ней носом
        bool isTurningAround = Vector3.Dot(distanceToTarget.normalized, CarTransform.forward) < BehindThreshold;

        float rotationSpeed = isTurningAround ? TurnaroundRotationSpeed : RotationSpeed;
        float targetSpeed = isTurningAround ? 0f : MaxSpeed;

        Speed = Mathf.MoveTowards(Speed, targetSpeed, Acceleration * Time.deltaTime);
        CarTransform.rotation = Quaternion.RotateTowards(CarTransform.rotation, lookAtTarget, rotationSpeed * Time.deltaTime);
        CarTransform.position += CarTransform.forward * Speed * Time.deltaTime;
    }
}
