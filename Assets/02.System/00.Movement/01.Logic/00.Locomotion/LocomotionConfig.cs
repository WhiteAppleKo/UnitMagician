using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using UnityEngine;
using CameraMovement;
using CameraMovement.UI;

namespace Movement.RefactoredLocomotion
{
    public class LocomotionConfig : MonoBehaviour
    {
        [Header("Locomotion Data")]
        [SerializeField] private PureDataLocomotion pureData;

        [Header("Camera Data")]
        [SerializeField] private PureDataCameraSetting cameraPureData;

        [Header("Visualizer Components")]
        [SerializeField] private LocomotionVisualizer visualizer;
        [SerializeField] private CameraFollowVisualizer cameraVisualizer;

        [Header("UI & Input Components")]
        [SerializeField] private InputReader inputReader;
        [SerializeField] private CameraOptionUIController cameraOptionUI;

        public PureDataLocomotion PureData => pureData;
        public PureDataCameraSetting CameraPureData => cameraPureData;
        public LocomotionVisualizer Visualizer => visualizer;
        public CameraFollowVisualizer CameraVisualizer => cameraVisualizer;
        public InputReader InputReader => inputReader;
        public CameraOptionUIController CameraOptionUI => cameraOptionUI;
    }
}
