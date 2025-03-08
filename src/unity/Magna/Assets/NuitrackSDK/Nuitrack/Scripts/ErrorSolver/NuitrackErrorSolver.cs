using UnityEngine;
#if UNITY_EDITOR_WIN
using UnityEditor;
#endif
using System;
using System.IO;

namespace NuitrackSDK.ErrorSolver
{
    public enum ErrorType
    {
        Unexpected,
        AccessDenied,
        SensorConnectionProblem,
        TBB,
        NoLicense,
        EndTrialLicense,
        ArchitectureMismatch,
        AndroidArchitectureMismatch,
        OldNuitrackRuntime,
        NuitrackAndroidNotInstalled,
        NuitrackAppAdditionalResources,
        NuitrackHomeVar,
        NuitrackModuleAccess,
        BadConfigValue,
    }

    public class NuitrackErrorSolver
    {
        public delegate void OnErrorHandler(ErrorType errorType, string errorText, string rawErrorText);
        public static event OnErrorHandler onError;

        static ErrorType errorType = ErrorType.Unexpected;

        public static string NuitrackBinPath
        {
            get
            {
                if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    string path = "";
                    if (Environment.GetEnvironmentVariable("NUITRACK_HOME") != null)
                        path = Path.Combine(Environment.GetEnvironmentVariable("NUITRACK_HOME"), "bin");
                    else
                        return null;
#if NUITRACK_PORTABLE
                    path = Path.Combine(Application.dataPath, "NuitrackSDK", "plugins", "x86_64");
#endif
                    return path;
                }
                else
                    return null;
            }
        }

        public static string NuitrackHomePath
        {
            get
            {
                if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
                {
                    string path = Environment.GetEnvironmentVariable("NUITRACK_HOME");
#if NUITRACK_PORTABLE
                    path = Path.Combine(Application.dataPath, "NuitrackSDK", "plugins", "x86_64");
#endif
                    return path;
                }
                else
                    return null;
            }
        }

        public static string CheckError(Exception ex, bool showInLog = true, bool showTroubleshooting = true)
        {
            return CheckError(ex.ToString(), showInLog, showTroubleshooting);
        }

        public static string CheckError(string error, bool showInLog = true, bool showTroubleshooting = true)
        {
            string troubleshootingPageMessage = 
                "Also look Nuitrack Troubleshooting page:github.com/3DiVi/nuitrack-sdk/blob/master/doc/Troubleshooting.md" +
                "\nIf all else fails and you decide to contact our technical support, do not forget to attach the Unity Log File (https://docs.unity3d.com/ScriptReference/Debug.Log.html or ADB) and specify the Nuitrack version";

            CheckGeneralErrors(error);

            if (errorType == ErrorType.Unexpected && (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor))
                CheckStandaloneErrors(error);

            if (errorType == ErrorType.Unexpected && Application.platform == RuntimePlatform.Android && !Application.isEditor)
                CheckAndroidErrors(error);

            string errorMessage = GetErrorText(errorType);

            if (showInLog)
                Debug.LogError(errorMessage);

            if (showInLog && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "AllModulesScene")
                Debug.LogError("It is recommended to test on AllModulesScene");

            if (showInLog && showTroubleshooting)
                Debug.LogError(troubleshootingPageMessage);

            if (showInLog)
                Debug.LogError(error);

            if (showTroubleshooting)
                errorMessage += "\n" + troubleshootingPageMessage;

            onError?.Invoke(errorType, errorMessage, error);

            return (errorMessage);
        }

        static void CheckGeneralErrors(string error)
        {
            if (error.Contains("Can't create DepthSensor"))
                errorType = ErrorType.SensorConnectionProblem;

            if (error.Contains("BadConfigValueException"))
                errorType = ErrorType.BadConfigValue;

            if (error.Contains("LicenseNotAcquiredException"))
            {
                errorType = ErrorType.NoLicense;

                if (NuitrackManager.Instance.LicenseInfo.Trial || NuitrackManager.Instance.RunningTime > 60)
                    errorType = ErrorType.EndTrialLicense;
            }
        }

        static void CheckEnvironmentVariables(string error)
        {
#if NUITRACK_PORTABLE
            return;
#endif

            if (NuitrackHomePath == null)
            {
                errorType = ErrorType.NuitrackHomeVar;
            }
            else
            {
                string nuitrackModulePath = NuitrackHomePath + "\\middleware\\NuitrackModule.dll";
                if (!File.Exists(nuitrackModulePath))
                {
                    errorType = ErrorType.NuitrackModuleAccess;
                }
            }
        }

        static string TBBError()
        {
            string errorMessage = "";
            string nuitrackTbbPath = "";

            nuitrackTbbPath = Path.Combine(NuitrackBinPath, "tbb.dll");
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
#if UNITY_EDITOR_WIN
                string unityTbbPath = EditorApplication.applicationPath.Replace("Unity.exe", "") + "tbb.dll";
                errorMessage = "You need to replace the file " + unityTbbPath + " with Nuitrack compatible file " + nuitrackTbbPath + " (Don't forget to close the editor first)";
#endif
            }
            else
            {
                errorMessage = "Problem with the file tbb.dll in the Nuitrack folder " + nuitrackTbbPath + ". Reinstall Nuitrack";
            }

            return errorMessage;
        }

        static void CheckAndroidErrors(string error)
        {
            if (error.Contains("INIT_NUITRACK_MANAGER_NOT_INSTALLED"))
                errorType = ErrorType.NuitrackAndroidNotInstalled;
            
            if (error.Contains("INIT_NUITRACK_RESOURCES_NOT_INSTALLED"))
                errorType = ErrorType.NuitrackAppAdditionalResources;
            
            if (error.Contains("nuitrack.ModuleNotInitializedException"))
                errorType = ErrorType.AndroidArchitectureMismatch;
        }

        static void CheckStandaloneErrors(string error)
        {
            try
            {
                CheckEnvironmentVariables(error);
                if (errorType != ErrorType.Unexpected)
                    return;

                if (error.Contains("TBB"))
                    errorType = ErrorType.TBB;
                
                if (error.Contains("nuitrack_SetParam"))
                    errorType = ErrorType.OldNuitrackRuntime;
                
                if (error.Contains("System.DllNotFoundException: libnuitrack"))
                    errorType = ErrorType.ArchitectureMismatch;
            }
            catch (Exception)
            {
                if (error.Contains("Cannot load library module"))
                    errorType = ErrorType.AccessDenied;
            }
        }

        static string GetErrorText(ErrorType type)
        {
            string errorMessage = "";
            string incorrectInstallingMessage =
                "1.Is Nuitrack installed at all? (github.com/3DiVi/nuitrack-sdk/tree/master/Platforms)\n" +
                "2.Try restart PC\n" +
                "3.Check your Environment Variables in Windows settings.\n" +
                "There should be two variables \"NUITRACK_HOME\" with a path to \"Nuitrack\\nuitrack\\nutrack\" and a \"Path\" with a path to %NUITRACK_HOME%\\bin " +
                "Maybe the installation probably did not complete correctly, in this case, look Troubleshooting Page.";

            switch (type)
            {
                case ErrorType.Unexpected:
                    string nuitrack_sample_test = "";
                    if (Application.platform != RuntimePlatform.WindowsEditor)
                        nuitrack_sample_test = "Does this example work? " + NuitrackBinPath + "\\bin\\nuitrack_sample.exe.";
                    errorMessage = "Perhaps the sensor is already being used in other program. Or some unexpected error.\n" + nuitrack_sample_test;
                    break;
                case ErrorType.AccessDenied:
                    errorMessage = 
                        GetAccessDeniedMessage(Path.Combine(NuitrackHomePath, "middleware")) + " " +
                        "Path: " + NuitrackBinPath +
                        "\nIf that doesn't work, check to see if you have used any other skeleton tracking software. If so, try uninstalling it and rebooting.";
                    break;
                case ErrorType.SensorConnectionProblem:
                    errorMessage =
                        "Can't create DepthSensor module. Sensor connected? Is the connection stable? Are the wires okay?\n" +
                        "If you use RealSense, then USB 3.0 is required for it to work. If the sensor is connected and you see this error, then check the sensor, the wire and the USB ports.";
                    break;
                case ErrorType.TBB:
                    errorMessage += TBBError();
                    break;
                case ErrorType.NoLicense:
                    errorMessage = "Activate Nuitrack license. Open Nuitrack App";
                    break;
                case ErrorType.EndTrialLicense:
                    errorMessage = "Nuitrack Trial time is over. Restart app. For unlimited time of use, you can switch to another license https://nuitrack.com/#pricing";
                    break;
                case ErrorType.ArchitectureMismatch:
                    errorMessage = 
                        "Perhaps installed Nuitrack Runtime version for x86 (nuitrack-windows-x86.exe), in this case, install x64 version " +
                        "(github.com/3DiVi/nuitrack-sdk/blob/master/Platforms/nuitrack-windows-x64.exe)";
                    break;
                case ErrorType.AndroidArchitectureMismatch:
                    errorMessage =
                        "Your application and Nuitrack application may have incompatible architectures. " +
                        "Check that the correct architecture is set in the player settings (only one) and the correct version of the Nuitrack App is downloaded https://github.com/3DiVi/nuitrack-sdk/tree/master/Platforms";
                    break;
                case ErrorType.OldNuitrackRuntime:
                    errorMessage = "Update Nuitrack Runtime https://github.com/3DiVi/nuitrack-sdk/tree/master/Platforms";
                    break;
                case ErrorType.NuitrackAndroidNotInstalled:
                    errorMessage = "Install VicoVR App from Google Play or Nuitrack App. https://github.com/3DiVi/nuitrack-sdk/tree/master/Platforms";
                    break;
                case ErrorType.NuitrackAppAdditionalResources:
                    errorMessage = "Launch Nuitrack application to install additional resources";
                    break;
                case ErrorType.NuitrackHomeVar:
                    errorMessage = "Environment Variable [NUITRACK_HOME] not found\n" + incorrectInstallingMessage;
                    break;
                case ErrorType.NuitrackModuleAccess:
                    string nuitrackMiddlewarePath = Path.Combine(NuitrackHomePath, "middleware");
                    errorMessage = 
                        "File: " + Path.Combine(nuitrackMiddlewarePath, "NuitrackModule.dll") + " not exists or Unity doesn't have enough rights to access it.\n" + 
                        "Nuitrack path is really: " + NuitrackHomePath + "?\n" + 
                        incorrectInstallingMessage + "\n" +
                        "4." + GetAccessDeniedMessage(nuitrackMiddlewarePath);
                    break;
                case ErrorType.BadConfigValue:
                    errorMessage = "Error in nuitrack.config!";
                    break;
                default:
                    break;
            }

            return errorMessage;
        }

        static string GetAccessDeniedMessage(string path)
        {
            string accessDeniedMessage = 
                "Check the read\\write permissions for the folder where Nuitrack Runtime is installed, as well as for all subfolders and files. " +
                "Can you create text-file in " + path + " folder?" + " Try allow Full controll permissions for Users.";

            return accessDeniedMessage;
        }

        static public bool CheckCudnn()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                return true;
#if UNITY_EDITOR_WIN
            string fileName = "cudnn64_7.dll";
            string editorCudnnPath = EditorApplication.applicationPath.Replace("Unity.exe", "");

            if (Environment.GetEnvironmentVariable("CUDA_PATH") == null)
            {
                string message = "CUDA not found" +
                                "\nCheck Nuitrack Object Detection requirements";

                if(EditorUtility.DisplayDialog("CUDA not found", message, "Open the documentation page", "Cancel"))
                {
                    Application.OpenURL("https://github.com/3DiVi/nuitrack-sdk/blob/master/doc/Nuitrack_AI.md#nuitrack-ai-object-detection");
                }

                return false;
            }

            string cudaCudnnPath = Path.Combine(Environment.GetEnvironmentVariable("CUDA_PATH"), "bin");

            if (!FileCompare(Path.Combine(cudaCudnnPath, fileName), Path.Combine(editorCudnnPath, fileName)))
            {
                string message = "1. Close the Unity editor" + "\n" +
                                "2. Copy the " + fileName + " library from " + cudaCudnnPath + " to your Unity editor folder " + editorCudnnPath + "\n" +
                                "3. Run the Unity editor again";
                EditorUtility.DisplayDialog(fileName, message, "OK");
                return false;
            }
#endif
            return true;
        }

        static bool TryGetFileLength(FileInfo file, out long fileLength)
        {
            fileLength = 0;
            try
            {
                using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    fileLength = file.Length;
                }
            }
            catch (IOException)
            {
                return true;
            }

            return false;
        }

        static bool FileCompare(string file1, string file2)
        {
            if (TryGetFileLength(new FileInfo(file1), out long file1Size) || TryGetFileLength(new FileInfo(file2), out long file2Size))
                return false;

            return file1Size == file2Size;
        }
    }
}