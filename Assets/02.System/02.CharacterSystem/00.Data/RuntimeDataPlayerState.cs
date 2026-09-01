using System;
using UnityEngine;

namespace CharacterSystem
{
    [Serializable]
    public class RuntimeDataPlayerState : ISaveableData
    {
        private readonly PureDataPlayerState pureData;
        private PlayerStateType currentState;
        private PlayerStateType previousState;

        public PureDataPlayerState PureData => pureData;
        public PlayerStateType CurrentState => currentState;
        public PlayerStateType PreviousState => previousState;

        public event Action<PlayerStateType, PlayerStateType> OnStateChanged;
        public event Action<PlayerStateType> OnStateEntered;
        public event Action<PlayerStateType> OnStateExited;

        public RuntimeDataPlayerState(PureDataPlayerState pureData)
        {
            this.pureData = pureData;
            currentState = pureData != null ? pureData.DefaultState : PlayerStateType.Normal;
            previousState = currentState;
        }

        public bool ChangeState(PlayerStateType newState)
        {
            if (currentState == newState) return false;

            var oldState = currentState;
            previousState = oldState;
            currentState = newState;

            OnStateExited?.Invoke(oldState);
            OnStateEntered?.Invoke(newState);
            OnStateChanged?.Invoke(oldState, newState);

            return true;
        }

        public string SaveToJson()
        {
            var dto = new SaveDTO
            {
                currentState = (int)currentState,
                previousState = (int)previousState
            };
            return JsonUtility.ToJson(dto);
        }

        public void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            var dto = JsonUtility.FromJson<SaveDTO>(json);
            if (dto == null) return;

            previousState = (PlayerStateType)dto.previousState;
            ChangeState((PlayerStateType)dto.currentState);
        }

        [Serializable]
        private class SaveDTO
        {
            public int currentState;
            public int previousState;
        }
    }
}
