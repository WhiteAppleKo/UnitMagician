using System;
using System.Collections.Generic;
using UnityEngine;
using CharacterSystem;

namespace PipeLine.Combat
{
    /// <summary>
    /// 진영 -> 물리 레이어 변환과 충돌 매트릭스 설정을 맡는 정적 헬퍼입니다(16-B). "누가 같은 편인가"의 출처는 FactionRelation 하나이며
    /// 이 클래스는 그 규칙을 레이어로 옮기고 적용만 합니다(규칙을 다시 정의하지 않는다).
    ///  - 몸체 레이어: Player/Ally -> BodyAlly, Enemy -> BodyEnemy, Neutral -> BodyNeutral (비캐릭터 오브젝트(돌문 등)는 몸체 레이어를 적용하지 않고 Default 유지 - 적용 주체는 CharacterStatComponent뿐)
    ///  - 공격 판정체 레이어(투사체/근접/범위): Player/Ally -> AttackAlly, Enemy -> AttackEnemy, Neutral -> AttackNeutral
    ///  - 충돌 매트릭스: FactionRelation.IsSameSide인 (공격 판정체 진영, 몸체 진영) 쌍의 레이어끼리만 충돌을 끈다
    ///    (AttackAlly-BodyAlly, AttackEnemy-BodyEnemy, AttackNeutral-BodyNeutral). 다른 쌍(몸체끼리, 공격 판정체끼리, 공격 판정체-Default)은 건드리지 않는다.
    ///  - 구르기 무적(19-B): IgnoreAttack 레이어는 진영과 무관한 단일 레이어이며 AttackAlly/AttackEnemy/AttackNeutral과의 충돌만 끈다
    ///    (벽/지면(Default)·몸체끼리 충돌은 유지). 구르기의 무적 구간에 몸체 레이어 대신 이 레이어를 적용하고 끝나면 그 시점 진영의 몸체 레이어로 복원한다.
    /// 레이어 이름은 프로젝트 설정(TagManager)에 정의돼 있다. 프로젝트 설정의 충돌 매트릭스는 손으로 바꾸지 않고 시작 시 코드가 한 번 설정한다.
    /// </summary>
    public static class FactionLayers
    {
        private const string BodyAllyName = "BodyAlly";
        private const string BodyEnemyName = "BodyEnemy";
        private const string BodyNeutralName = "BodyNeutral";
        private const string AttackAllyName = "AttackAlly";
        private const string AttackEnemyName = "AttackEnemy";
        private const string AttackNeutralName = "AttackNeutral";
        private const string IgnoreAttackName = "IgnoreAttack";

        private static readonly List<Collider> colliderBuffer = new List<Collider>();

        /// <summary>진영에 맞는 몸체 레이어 번호. Neutral은 BodyNeutral이다(편을 알 수 없으면 Default).</summary>
        public static int GetBodyLayer(FactionType faction)
        {
            switch (FactionRelation.GetSide(faction))
            {
                case FactionSide.PlayerSide: return LayerMask.NameToLayer(BodyAllyName);
                case FactionSide.EnemySide: return LayerMask.NameToLayer(BodyEnemyName);
                case FactionSide.NeutralSide: return LayerMask.NameToLayer(BodyNeutralName);
                default: return 0;
            }
        }

        /// <summary>진영에 맞는 공격 판정체 레이어 번호. Neutral은 AttackNeutral이다(편을 알 수 없으면 Default).</summary>
        public static int GetAttackLayer(FactionType faction)
        {
            switch (FactionRelation.GetSide(faction))
            {
                case FactionSide.PlayerSide: return LayerMask.NameToLayer(AttackAllyName);
                case FactionSide.EnemySide: return LayerMask.NameToLayer(AttackEnemyName);
                case FactionSide.NeutralSide: return LayerMask.NameToLayer(AttackNeutralName);
                default: return 0;
            }
        }

        /// <summary>구르기 무적(19-B)에서 몸체 레이어 대신 쓰는 IgnoreAttack 레이어 번호. 모든 공격 판정체 레이어와의 충돌이 꺼져 있다.</summary>
        public static int GetIgnoreAttackLayer()
        {
            return LayerMask.NameToLayer(IgnoreAttackName);
        }

        /// <summary>루트와 모든 자식 콜라이더(비활성/CharacterController 포함)의 GameObject 레이어를 같은 값으로 바꾼다.</summary>
        public static void ApplyLayerRecursively(GameObject root, int layer)
        {
            if (root == null) return;

            if (root.layer != layer) root.layer = layer;

            root.GetComponentsInChildren(true, colliderBuffer);
            for (int i = 0; i < colliderBuffer.Count; i++)
            {
                var colliderObject = colliderBuffer[i].gameObject;
                if (colliderObject.layer != layer) colliderObject.layer = layer;
            }

            colliderBuffer.Clear();
        }

        /// <summary>
        /// 시작 시 한 번 호출하는 충돌 매트릭스 설정. 모든 (공격 판정체 진영, 몸체 진영) 조합 중 FactionRelation.IsSameSide인 쌍의 레이어만 충돌을 끈다.
        /// 같은 값을 여러 번 설정해도 결과가 같다(멱등).
        /// </summary>
        public static void ConfigureCollisionMatrix()
        {
            var factions = (FactionType[])Enum.GetValues(typeof(FactionType));
            for (int p = 0; p < factions.Length; p++)
            {
                for (int b = 0; b < factions.Length; b++)
                {
                    if (!FactionRelation.IsSameSide(factions[p], factions[b])) continue;

                    Physics.IgnoreLayerCollision(GetAttackLayer(factions[p]), GetBodyLayer(factions[b]), true);
                }
            }

            // 19-B: IgnoreAttack은 모든 공격 판정체 레이어(Ally/Enemy/Neutral)와의 충돌만 끈다. 몸체끼리·Default와는 건드리지 않는다.
            int ignoreAttackLayer = GetIgnoreAttackLayer();
            Physics.IgnoreLayerCollision(ignoreAttackLayer, LayerMask.NameToLayer(AttackAllyName), true);
            Physics.IgnoreLayerCollision(ignoreAttackLayer, LayerMask.NameToLayer(AttackEnemyName), true);
            Physics.IgnoreLayerCollision(ignoreAttackLayer, LayerMask.NameToLayer(AttackNeutralName), true);
        }
    }
}
