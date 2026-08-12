using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SceneSwitch : MonoBehaviour
{
    [SerializeField] private string _sceneName;

    private Button _button;

    private void OnEnable()
    {
        _button.onClick.AddListener(() => SwitchScene(_sceneName));
    }

    private void OnDisable()
    {
        _button.onClick.RemoveAllListeners();
    }

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    private void SwitchScene(string name)
    {
        SceneManager.LoadScene(name);
    }
}
