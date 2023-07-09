using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIScreen : MonoBehaviour
{
    // Start is called before the first frame update
    public void Start()
    {

    }

    // Update is called once per frame
    public void Update()
    {

    }

    public void Show()
    {
        gameObject.SetActive(true);
        LeanTween.scale(gameObject, Vector3.one, 0.5f).setEase(LeanTweenType.easeOutBack);
        LeanTween.alpha(gameObject, 1, 0.5f).setEase(LeanTweenType.easeOutBack);
    }

    public void Hide()
    {
        LeanTween.scale(gameObject, Vector3.zero, 0.5f).setEase(LeanTweenType.easeOutBack);
        LeanTween.alpha(gameObject, 0, 0.5f).setEase(LeanTweenType.easeOutBack);
        gameObject.SetActive(false);
    }
}
