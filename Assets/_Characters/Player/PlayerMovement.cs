﻿using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using RPG.CameraUI;


// TODO , extract weaponSystem
namespace RPG.Characters
{
    public class PlayerMovement : MonoBehaviour //no Idamageable because we are going fron interface to component
    {
        [SerializeField] float baseDamage = 10f;
        [SerializeField] AnimatorOverrideController animatorOverrideController;
        [SerializeField] Weapon currentWeaponConfig;


        [Range(0.1f, 1.0f)] [SerializeField] float criticalHitChance = 0.1f;
        [SerializeField] float criticalHitMultiplier = 1.25f;
        [SerializeField] ParticleSystem criticalHitParticle;


        Enemy enemy = null;
        private float lastHitTime = 0f;
        Animator animator;
        const String ATTACK_TRIGGER = "Attack";
        SpecialAbilities abilities;
        CameraRaycaster cameraRaycaster;
        GameObject weaponObject;
        Character character;

        private bool isGamePaused;
        public LevelFlowManager levelFlowManager;

        private void Start()
        {
            character = GetComponent<Character>();
            isGamePaused = false;
            abilities = GetComponent<SpecialAbilities>();
            RegisterForMouseEvents();
            PutWeaponInHand(currentWeaponConfig);
            SetAttackAnimation();
        }

        private void RegisterForMouseEvents()
        {
            //cameraRaycaster = Camera.main.GetComponent<CameraRaycaster>();
            cameraRaycaster = FindObjectOfType<CameraRaycaster>();
            cameraRaycaster.onMouseOverEnemy += OnMouseOverEnemy; //Delegate
            cameraRaycaster.onMouseOverpotentiallyWalkable += onMouseOverpotentiallyWalkable;
        }

        private void Update()
        {         
            ScanForAbilityKeyDown();         
            PauseGame();
        }

        void onMouseOverpotentiallyWalkable(Vector3 destination)
        {
            if(Input.GetMouseButton(0))
            {
                character.SetDestination(destination);
            }
        }

        void OnMouseOverEnemy(Enemy enemyToSet)
        {
            this.enemy = enemyToSet;
            if (Input.GetMouseButton(0) && IsTargetInRange(enemyToSet.gameObject))
            {
                AttackTarget();
            }
            if (Input.GetMouseButtonDown(1))
            {
                abilities.AttemptSpecialAbility(0);
            }
        }

        private void ScanForAbilityKeyDown()
        {
            
            for (int keyIndex = 1; keyIndex < abilities.GetNumberOfAbilities(); keyIndex++)
            {
                if (Input.GetKeyDown(keyIndex.ToString()))
                {                    
                    abilities.AttemptSpecialAbility(keyIndex);
                }
            }
        }

        private void SetAttackAnimation()
        {
            animator = GetComponent<Animator>();
            animator.runtimeAnimatorController = animatorOverrideController;
            animatorOverrideController["DEFAULT ATTACK"] = currentWeaponConfig.GetAnimClip();
        }

        private GameObject RequestDominantHand()
        {
            var dominantHands = GetComponentsInChildren<DominantHand>();
            int numberOfDominantHands = dominantHands.Length;
            Assert.IsFalse(numberOfDominantHands <= 0, "No Domiannt hand found on the player , please add one .");
            Assert.IsFalse(numberOfDominantHands > 1, "Multiple Dominant hand scripts on the player , please remove one");
            return dominantHands[0].gameObject;
        }

        private void AttackTarget()
        {
            if (Time.time - lastHitTime > currentWeaponConfig.GetMinTimeBetweenHits())
            {
                SetAttackAnimation();
                animator = GetComponent<Animator>();
                animator.SetTrigger(ATTACK_TRIGGER);
                lastHitTime = Time.time;
            }
        }

        private float CalculateDamage()
        {
            // allow critical hit 
            bool isCriticalHit = UnityEngine.Random.Range(0f, 1f) <= criticalHitChance;
            float damageBeforeCritical = baseDamage + currentWeaponConfig.GetAdditionalDamage();
            if (isCriticalHit)
            {
                criticalHitParticle.Play();
                return damageBeforeCritical * criticalHitMultiplier;
            }
            else
            {
                return damageBeforeCritical;
            }
        }

        private bool IsTargetInRange(GameObject target)
        {
            float distanceToTarget = (target.transform.position - transform.position).magnitude;
            return distanceToTarget <= currentWeaponConfig.GetMaxAttackRange();
        }


        public void PutWeaponInHand(Weapon weaponToUse)
        {
            currentWeaponConfig = weaponToUse;
            var weaponPrefab = weaponToUse.GetWeaponPrefab();
            GameObject dominantHand = RequestDominantHand();
            Destroy(weaponObject);
            weaponObject = Instantiate(weaponPrefab, dominantHand.transform);
            weaponObject.transform.localPosition = currentWeaponConfig.grip.localPosition;
            weaponObject.transform.localRotation = currentWeaponConfig.grip.localRotation;
        }
        public void OnButtonEven()   //TODO , move to anotehr script
        {
            SceneManager.LoadScene(0);
        }

        private void PauseGame()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                isGamePaused = !isGamePaused;
            }
            if (isGamePaused && Input.GetKeyDown(KeyCode.Escape))
            {
                Time.timeScale = 0;
                levelFlowManager.pauseGame.SetActive(true);

            }
            else if (!isGamePaused && Input.GetKeyDown(KeyCode.Escape))
            {
                Time.timeScale = 1;
                levelFlowManager.pauseGame.SetActive(false);
            }
        }
    }
}