using UnityEngine;

[CreateAssetMenu(fileName = "RobotStateEvent", menuName = "Events/Robot State Event")]
public class RobotStateEvent : ScriptableObject
{
    public delegate void StateChangeHandler(string newState);
    public event StateChangeHandler OnStateChange;

    public void RaiseEvent(string newState)
    {
        OnStateChange?.Invoke(newState);
    }
}
