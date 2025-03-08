using UnityEngine;


namespace NuitrackSDK.Tutorials.FaceTracker
{
    public class BlendshapeWeights
    {
        float lerpSpeed = 7f;

        float jawParam = 0;

        float leftEyeParam = 0;
        float rightEyeParam = 0;

        float leftBrowUpParam = 0;
        float rightBrowUpParam = 0;

        float smile = 0.0f;

        public float GetJawOpen(nuitrack.Face face)
        {
            float jawOpen = face.GetEmotionValue(nuitrack.Emotions.Type.surprise);

            if (jawOpen >= 0.51f)
            {
                jawOpen = 1.0f;
            }
            else if (jawOpen >= 0.25f)
            {
                jawOpen = 0.5f;
            }
            else
            {
                jawOpen = 0.0f;
            }
            jawParam = Mathf.Lerp(jawParam, jawOpen, lerpSpeed * Time.deltaTime);

            return jawParam * 100;
        }

        public float GetEyeBlinkLeft(nuitrack.Face face)
        {
            float eyeOpen = face.GetEmotionValue(nuitrack.Emotions.Type.angry);

            if (eyeOpen > 0.5f)
            {
                leftEyeParam = Mathf.Lerp(leftEyeParam, 0.7f, lerpSpeed * Time.deltaTime);
            }
            else
            {
                leftEyeParam = Mathf.Lerp(leftEyeParam, 0, lerpSpeed * Time.deltaTime);
            }

            return leftEyeParam * 100;
        }

        public float GetEyeBlinkRight(nuitrack.Face face)
        {
            float eyeOpen = face.GetEmotionValue(nuitrack.Emotions.Type.angry);

            if (eyeOpen > 0.5f)
            {
                rightEyeParam = Mathf.Lerp(rightEyeParam, 0.7f, lerpSpeed * Time.deltaTime);
            }
            else
            {
                rightEyeParam = Mathf.Lerp(rightEyeParam, 0, lerpSpeed * Time.deltaTime);
            }

            return rightEyeParam * 100;
        }

        public float GetAngry(nuitrack.Face face)
        {
            return face.GetEmotionValue(nuitrack.Emotions.Type.angry) * 100;
        }

        public float GetSmile(nuitrack.Face face)
        {
            float smileParam = Mathf.Lerp(smile, face.GetEmotionValue(nuitrack.Emotions.Type.happy), lerpSpeed * Time.deltaTime);
            smile = face.GetEmotionValue(nuitrack.Emotions.Type.happy);
            return smileParam * 100;
        }

        public float GetBrowUpLeft(nuitrack.Face face)
        {
            float BrowLeftUp = face.GetEmotionValue(nuitrack.Emotions.Type.surprise);

            if (BrowLeftUp >= 0.5f)
                BrowLeftUp = 1.0f;
            else
                BrowLeftUp = 0.0f;

            leftBrowUpParam = Mathf.Lerp(leftBrowUpParam, BrowLeftUp, lerpSpeed * Time.deltaTime);

            return leftBrowUpParam * 100;
        }

        public float GetBrowUpRight(nuitrack.Face face)
        {
            float BrowRightUp = face.GetEmotionValue(nuitrack.Emotions.Type.surprise);

            if (BrowRightUp >= 0.5f)
                BrowRightUp = 1.0f;
            else
                BrowRightUp = 0.0f;

            rightBrowUpParam = Mathf.Lerp(rightBrowUpParam, BrowRightUp, lerpSpeed * Time.deltaTime);

            return rightBrowUpParam * 100;
        }
    }
}