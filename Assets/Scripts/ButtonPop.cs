using UnityEngine;
using UnityEngine.EventSystems;

// los botones crecen un poco al pasar el raton o al seleccionarlos con el teclado
public class ButtonPop : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    bool hot;

    void OnEnable() { hot = false; transform.localScale = Vector3.one; }
    public void OnPointerEnter(PointerEventData e) => hot = true;
    public void OnPointerExit(PointerEventData e) => hot = false;
    public void OnSelect(BaseEventData e) => hot = true;
    public void OnDeselect(BaseEventData e) => hot = false;

    void Update()
    {
        float k = hot ? 1.07f : 1f;
        transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(k, k, 1f), 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f));
    }
}
