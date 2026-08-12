using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraWaypointTour : MonoBehaviour
{
    [Serializable]
    public class Waypoint
    {
        [Tooltip("Объект в сцене, к которому летит камера. Позиция и поворот берутся с него")]
        public Transform target;

        [Tooltip("Позиция точки в мировых координатах. Используется, только если Target не задан")]
        public Vector3 position;

        [Tooltip("Углы Эйлера камеры в этой точке. Если Target задан — добавляются к его повороту")]
        public Vector3 eulerRotation;

        [Tooltip("Сколько секунд лететь ДО этой точки от предыдущей. Поворот при этом не меняется")]
        public float travelTime = 2f;

        [Tooltip("Сколько секунд доворачиваться в точке до её поворота. 0 — мгновенно")]
        public float rotateTime = 0.5f;

        [Tooltip("Сколько секунд стоять В этой точке после доворота")]
        public float holdTime = 1f;

        [Tooltip("Кривая сглаживания перелёта и доворота (0..1 -> 0..1)")]
        public AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public Vector3 Position => target != null ? target.position : position;

        // Поворот таргета (если он есть) плюс заданный в инспекторе доворот
        public Quaternion Rotation => target != null
            ? target.rotation * Quaternion.Euler(eulerRotation)
            : Quaternion.Euler(eulerRotation);
    }

    [SerializeField] private List<Waypoint> waypoints = new List<Waypoint>();

    [Tooltip("Зациклить маршрут")]
    [SerializeField] private bool loop = true;

    [Tooltip("Стартовать автоматически при включении объекта")]
    [SerializeField] private bool playOnAwake = true;

    [Tooltip("Начинать движение из текущего положения камеры, а не телепортироваться в точку 0")]
    [SerializeField] private bool startFromCurrentPose = false;

    [Tooltip("Игнорировать Time.timeScale (полезно для меню/паузы)")]
    [SerializeField] private bool useUnscaledTime = false;

    [Tooltip("Во время стоянки держаться за Target, если объект двигается")]
    [SerializeField] private bool stickToTargetWhileHolding = true;

    public event Action<int> OnWaypointReached;
    public event Action OnTourFinished;

    private Coroutine routine;

    public bool IsPlaying => routine != null;
    public int CurrentIndex { get; private set; }

    private void OnEnable()
    {
        if (playOnAwake) Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        Stop();
        routine = StartCoroutine(TourRoutine());
    }

    public void Stop()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    private IEnumerator TourRoutine()
    {
        int startIndex = 0;
        Vector3 fromPos;

        if (startFromCurrentPose)
        {
            fromPos = transform.position;
        }
        else
        {
            // Мгновенно ставим камеру в первую точку и отстаиваем там holdTime
            ApplyPose(waypoints[0]);
            CurrentIndex = 0;
            OnWaypointReached?.Invoke(0);
            yield return Hold(waypoints[0]);

            fromPos = transform.position;
            startIndex = 1;
        }

        do
        {
            for (int i = startIndex; i < waypoints.Count; i++)
            {
                Waypoint wp = waypoints[i];

                // --- Перелёт: меняется только позиция, поворот остаётся прежним ---
                float duration = Mathf.Max(0f, wp.travelTime);
                if (duration > 0f)
                {
                    float t = 0f;
                    while (t < duration)
                    {
                        t += DeltaTime;
                        float k = Mathf.Clamp01(t / duration);

                        // Цель читаем каждый кадр: объект в сцене может двигаться
                        transform.position = Vector3.LerpUnclamped(fromPos, wp.Position, Ease(wp, k));
                        yield return null;
                    }
                }

                transform.position = wp.Position;
                CurrentIndex = i;
                OnWaypointReached?.Invoke(i);

                // --- Доворот в точке до её ротации ---
                yield return RotateTo(wp);

                // --- Стоянка ---
                yield return Hold(wp);

                fromPos = transform.position;
            }

            // На втором и последующих кругах снова идём с точки 0
            startIndex = 0;

        } while (loop);

        routine = null;
        OnTourFinished?.Invoke();
    }

    private float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    private static float Ease(Waypoint wp, float k) => wp.ease != null ? wp.ease.Evaluate(k) : k;

    // Поворот камеры уже в точке: позиция при этом держится на точке
    private IEnumerator RotateTo(Waypoint wp)
    {
        float duration = Mathf.Max(0f, wp.rotateTime);
        if (duration <= 0f)
        {
            ApplyPose(wp);
            yield break;
        }

        Quaternion fromRot = transform.rotation;
        float t = 0f;
        while (t < duration)
        {
            t += DeltaTime;
            float k = Mathf.Clamp01(t / duration);

            transform.position = wp.Position;
            transform.rotation = Quaternion.SlerpUnclamped(fromRot, wp.Rotation, Ease(wp, k));
            yield return null;
        }

        // Снимаем накопленную погрешность
        ApplyPose(wp);
    }

    // Стоянка в точке. Если объект-цель двигается, камера остаётся на нём
    private IEnumerator Hold(Waypoint wp)
    {
        float seconds = wp.holdTime;
        if (seconds <= 0f) yield break;

        bool stick = stickToTargetWhileHolding && wp.target != null;

        float t = 0f;
        while (t < seconds)
        {
            t += DeltaTime;
            if (stick) ApplyPose(wp);
            yield return null;
        }
    }

    private void ApplyPose(Waypoint wp)
    {
        transform.position = wp.Position;
        transform.rotation = wp.Rotation;
    }

    // --- Удобства в редакторе ---

    [ContextMenu("Добавить точку из текущего положения камеры")]
    private void AddWaypointFromCurrentTransform()
    {
        waypoints.Add(new Waypoint
        {
            position = transform.position,
            eulerRotation = transform.eulerAngles,
            travelTime = 2f,
            rotateTime = 0.5f,
            holdTime = 1f
        });
    }

    [ContextMenu("Создать объект-точку в текущем положении камеры")]
    private void CreateWaypointObject()
    {
        var go = new GameObject("CamPoint_" + waypoints.Count);
        go.transform.SetPositionAndRotation(transform.position, transform.rotation);

        waypoints.Add(new Waypoint
        {
            target = go.transform,
            travelTime = 2f,
            rotateTime = 0.5f,
            holdTime = 1f
        });
    }

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;

        Gizmos.color = Color.cyan;
        Vector3 prev = Vector3.zero;
        bool hasPrev = false;

        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector3 p = waypoints[i].Position;
            Gizmos.DrawWireSphere(p, 0.25f);

            // Направление взгляда в точке
            Vector3 fwd = waypoints[i].Rotation * Vector3.forward;
            Gizmos.DrawLine(p, p + fwd * 1.5f);

            if (hasPrev) Gizmos.DrawLine(prev, p);

            prev = p;
            hasPrev = true;
        }

        if (loop && waypoints.Count > 1)
            Gizmos.DrawLine(waypoints[waypoints.Count - 1].Position, waypoints[0].Position);
    }
}
