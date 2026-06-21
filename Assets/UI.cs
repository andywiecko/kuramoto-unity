using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UI : MonoBehaviour
{
    [SerializeField] private Kuramoto kuramoto;

    private void Start()
    {
        var doc = GetComponent<UIDocument>();
        var root = doc.rootVisualElement;
        
        var omega = root.Q<Slider>("omegaSlider");
        var omegaLabel = root.Q<Label>("omegaValue");
        omegaLabel.text = kuramoto.omega.ToString("F1");
        omega.RegisterValueChangedCallback(_ => { 
            kuramoto.omega = omega.value; 
            omegaLabel.text = omega.value.ToString("F1");
        });

        var K = root.Q<Slider>("KSlider");
        var KLabel = root.Q<Label>("KValue");
        KLabel.text = kuramoto.K.ToString("F1");
        K.RegisterValueChangedCallback(_ => {
            kuramoto.K = K.value;
            KLabel.text = K.value.ToString("F1");
        });

        var alpha = root.Q<Slider>("alphaSlider");
        var alphaLabel = root.Q<Label>("alphaValue");
        alphaLabel.text = kuramoto.alpha.ToString("F2");
        alpha.RegisterValueChangedCallback(_ => {
            kuramoto.alpha = alpha.value;
            alphaLabel.text = alpha.value.ToString("F2");
        });

        var button = root.Q<Button>("Randomize");
        button.clicked += kuramoto.Randomize;
    }
}
