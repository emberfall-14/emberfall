﻿namespace Content.IntegrationTests.Tests.Destructible
{
    public static class DestructibleTestPrototypes
    {
        public const string SpawnedEntityId = "DestructibleTestsSpawnedEntity";
        public const string DestructibleEntityId = "DestructibleTestsDestructibleEntity";
        public const string DestructibleDestructionEntityId = "DestructibleTestsDestructibleDestructionEntity";
        public const string DestructibleDamageTypeEntityId = "DestructibleTestsDestructibleDamageTypeEntity";
        public const string DestructibleDamageClassEntityId = "DestructibleTestsDestructibleDamageClassEntity";

        public static readonly string Prototypes = $@"
- type: entity
  id: {SpawnedEntityId}
  name: {SpawnedEntityId}

- type: entity
  id: {DestructibleEntityId}
  name: {DestructibleEntityId}
  components:
  - type: Damageable
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTrigger
        damage: 20
        triggersOnce: false
    - trigger:
        !type:DamageTrigger
        damage: 50
        triggersOnce: false
      behaviors:
      - !type:PlaySoundBehavior
        sound: /Audio/Effects/woodhit.ogg
      - !type:SpawnEntitiesBehavior
        spawn:
          {SpawnedEntityId}:
            min: 1
            max: 1
      - !type:DoActsBehavior
        acts: [""Breakage""]
  - type: TestThresholdListener

- type: entity
  id: {DestructibleDestructionEntityId}
  name: {DestructibleDestructionEntityId}
  components:
  - type: Damageable
  - type: Destructible
    thresholds:
    - trigger:
        !type:DamageTrigger
        damage: 50
      behaviors:
      - !type:PlaySoundBehavior
        sound: /Audio/Effects/woodhit.ogg
      - !type:SpawnEntitiesBehavior
        spawn:
          {SpawnedEntityId}:
            min: 1
            max: 1
      - !type:DoActsBehavior # This must come last as it destroys the entity.
        acts: [""Destruction""]
  - type: TestThresholdListener

- type: entity
  id: {DestructibleDamageTypeEntityId}
  name: {DestructibleDamageTypeEntityId}
  components:
  - type: Damageable
  - type: Destructible
    thresholds:
    - trigger:
        !type:AndTrigger
        triggers:
        - !type:DamageTypeTrigger
          damage: 10
          type: Blunt
        - !type:DamageTypeTrigger
          damage: 10
          type: Slash
  - type: TestThresholdListener

- type: entity
  id: {DestructibleDamageClassEntityId}
  name: {DestructibleDamageClassEntityId}
  components:
  - type: Damageable
  - type: Destructible
    thresholds:
    - trigger:
        !type:AndTrigger
        triggers:
        - !type:DamageClassTrigger
          damage: 10
          class: Burn
        - !type:DamageClassTrigger
          damage: 10
          class: Brute
  - type: TestThresholdListener";
    }
}
