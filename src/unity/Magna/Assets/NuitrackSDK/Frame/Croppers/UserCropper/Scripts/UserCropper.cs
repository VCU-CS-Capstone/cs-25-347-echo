using UnityEngine;


namespace NuitrackSDK.Frame
{
    [AddComponentMenu("NuitrackSDK/Frame/Cropper/User Cropper")]
    public class UserCropper : Cropper
    {
        protected override bool IsUserLost(UserData userData)
        {
            return userData == null || NuitrackManager.sensorsData[0].DepthFrame == null;
        }

        protected override Rect GetFrameRect(UserData userData, float width, float height)
        {
            return userData.BoundingBox(width, height);
        }
    }
}
