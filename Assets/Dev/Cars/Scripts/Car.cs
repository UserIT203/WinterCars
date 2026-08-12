using UnityEngine;
using UnityEngine.EventSystems;

public class Car : MonoBehaviour, IPointerClickHandler
{
    // Машина, за рулём которой сейчас игрок. На сцене она одна
    private static Car _playerCar;

    [SerializeField] private bool _isAI = true;

    [Header("Car Settings"), Space(5f)]
    [SerializeField] private float _maxSpeed;
    [SerializeField] private float _acceleration;

    [Header("AI Settings"), Space(5f)]
    [SerializeField] private RoadNode _startNode;
    [SerializeField] private float _arrivedDistance;

    private ICarMovement _movement;

    public bool IsAI => _isAI;

    public static Car PlayerCar => _playerCar;

    private void Start()
    {
        // На старте узел маршрута задан в инспекторе
        CreateMovement(_startNode);

        if(_isAI == false) _playerCar = this;
    }

    private void Update()
    {
        _movement.Move();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(_isAI == false) return;

        if(_playerCar != null && _playerCar != this) _playerCar.SwitchToAI();

        SwitchToPlayer();
    }

    private void SwitchToPlayer()
    {
        _isAI = false;
        CreateMovement(null);
        _playerCar = this;
    }

    private void SwitchToAI()
    {
        _isAI = true;

        CreateMovement(FindNearestNode());

        if(_playerCar == this) _playerCar = null;
    }

    private void CreateMovement(RoadNode startNode)
    {
        if(_isAI == true)
        {
            _movement = new MovementAI
                (
                    _maxSpeed,
                    _acceleration,
                    startNode,
                    _arrivedDistance,
                    transform
                );
        }
        else
        {
            _movement = new PlayerMovement
                (
                    _maxSpeed,
                    _acceleration,
                    transform
                );
        }
    }

    private RoadNode FindNearestNode()
    {
        RoadNode nearest = null;
        float bestDistance = float.MaxValue;

        foreach(RoadNode node in FindObjectsByType<RoadNode>(FindObjectsSortMode.None))
        {
            float distance = (node.Position - transform.position).sqrMagnitude;
            if(distance >= bestDistance) continue;

            bestDistance = distance;
            nearest = node;
        }

        return nearest != null ? nearest : _startNode;
    }
}
