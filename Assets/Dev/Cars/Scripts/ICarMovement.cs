using UnityEngine;

public interface ICarMovement
{
    public float Speed { get; }
    public float MaxSpeed { get; }
    public float Acceleration { get; }

    public Transform CarTransform { get; }
    public void Move();
}
