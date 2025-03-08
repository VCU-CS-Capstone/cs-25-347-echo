using UnityEngine;
using UnityEngine.UI;
using NuitrackSDK.ErrorSolver;

namespace NuitrackSDK
{
    public class ErrorDisplay : MonoBehaviour
    {
        [SerializeField] GameObject errorScreen;
        [SerializeField] Text errorUIText;
        [SerializeField] Text rawErrorUIText;

        void Awake()
        {
            errorScreen.SetActive(false);
        }

        void OnEnable()
        {
            NuitrackErrorSolver.onError += OnError;
        }

        void OnError(ErrorType errorType, string errorText, string rawErrorText)
        {
            errorScreen.SetActive(true);
            errorUIText.text = errorText;
            rawErrorUIText.text = rawErrorText;
        }

        void OnDisable()
        {
            NuitrackErrorSolver.onError -= OnError;
        }
    }
}
