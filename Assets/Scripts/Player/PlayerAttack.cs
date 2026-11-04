using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    //relaciona el ItemData del arma con su modelo en la mano y su punto de golpe
    [Serializable]
    private class WeaponVisual
    {
        public ItemData item;//wood_club, reinforced_wood_club, etc
        public GameObject model;//bat, bat_Bloody (hijos de la mano)
        public Transform attackPoint;//si queda vacio usa el attackPoint general
    }

    //input
    [SerializeField] private InputActionReference attackAction;
    //referencias
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Health health;
    [SerializeField] private QuickAccessController quickAccess;
    [SerializeField] private HUDController hud;
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private float blockAfterMenuTime = 0.15f;//tiempo sin poder atacar despues de cerrar el selector
    //armas
    [Header("Armas")]
    [SerializeField] private WeaponVisual[] weaponVisuals;
    //config del golpe
    [SerializeField] private Transform attackPoint;//fallback si el arma no tiene uno propio
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private bool faceCameraOnAttack = true;
    //audio (el AudioSource del player tiene que tener Output en el grupo SFX del MainMixer)
    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip hitClip;//golpe que recibe un zombie
    [SerializeField] private AudioClip finalHitClip;//golpe que mata al zombie
    [SerializeField] private AudioClip missClip;//ataque que no toca a nadie
    //privadas
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private readonly HashSet<IDamageable> alreadyHit = new();
    private float currentDamage;//daño del arma con la que se inicio el ataque
    private Transform currentAttackPoint;
    private float attackBlockedUntil;
    //publicas
    public bool IsAttacking { get; private set; }

    private void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!playerController) playerController = GetComponent<PlayerController>();
        if (!health) health = GetComponent<Health>();
        if (!sfxSource) sfxSource = GetComponent<AudioSource>();
        if (!quickAccess) quickAccess = FindFirstObjectByType<QuickAccessController>();
        if (!hud) hud = FindFirstObjectByType<HUDController>();
        if (!inventoryUI) inventoryUI = FindFirstObjectByType<InventoryUI>();
    }

    private void Start() => RefreshWeaponVisual();

    private void OnEnable()
    {
        attackAction.action.Enable();

        if (quickAccess != null)
        {
            quickAccess.OnQuickAccessChanged += RefreshWeaponVisual;
            quickAccess.OnSelectionChanged += OnSelectionChanged;
        }

        RefreshWeaponVisual();
    }

    private void OnDisable()
    {
        attackAction.action.Disable();
        if (playerController) playerController.MovementLocked = false;

        if (quickAccess != null)
        {
            quickAccess.OnQuickAccessChanged -= RefreshWeaponVisual;
            quickAccess.OnSelectionChanged -= OnSelectionChanged;
        }
    }

    private void Update()
    {
        if (GameProgressController.Instance != null && GameProgressController.Instance.IsFinaleLocked) return;

        if (health && health.IsDead) return;

        if (playerController != null && (playerController.IsStunned || playerController.IsBusy)) return;

        //con el selector rapido o el inventario abierto los clicks son para la UI
        if ((hud != null && hud.IsQuickSelectorOpen) || (inventoryUI != null && inventoryUI.IsInventoryOpen))
        {
            attackBlockedUntil = Time.unscaledTime + blockAfterMenuTime;
            return;
        }

        if (Time.unscaledTime < attackBlockedUntil) return;

        if (attackAction.action.WasPressedThisFrame() && !IsAttacking)
            StartAttack();
    }

    private void OnSelectionChanged(int _)
    {
        //si quedo un ataque en cola al cambiar de arma se descarta
        if (!IsAttacking && animator) animator.ResetTrigger(AttackHash);

        RefreshWeaponVisual();
    }

    //busca el arma seleccionada en el quick access. sin arma = manos vacias
    private bool TryGetEquippedWeapon(out WeaponData data, out WeaponVisual visual)
    {
        data = null;
        visual = null;

        if (quickAccess == null) return false;

        InventorySlot slot = quickAccess.SelectedSlot;
        if (slot == null || slot.IsEmpty) return false;
        if (!slot.item.TryGetWeaponData(out data)) return false;

        visual = GetVisual(slot.item);
        return true;
    }

    private WeaponVisual GetVisual(ItemData item)
    {
        if (weaponVisuals == null || item == null) return null;

        foreach (WeaponVisual v in weaponVisuals)
            if (v != null && v.item == item) return v;

        return null;
    }

    //muestra solo el modelo del arma seleccionada
    private void RefreshWeaponVisual()
    {
        if (weaponVisuals == null) return;

        TryGetEquippedWeapon(out _, out WeaponVisual equipped);

        foreach (WeaponVisual v in weaponVisuals)
        {
            if (v == null || v.model == null) continue;
            v.model.SetActive(v == equipped);
        }
    }

    private void StartAttack()
    {
        //sin arma no se hace nada
        if (!TryGetEquippedWeapon(out WeaponData data, out WeaponVisual visual)) return;

        IsAttacking = true;
        currentDamage = data.damage;
        currentAttackPoint = visual != null && visual.attackPoint != null ? visual.attackPoint : attackPoint;

        if (faceCameraOnAttack && cameraPivot)
            transform.rotation = Quaternion.Euler(0f, cameraPivot.eulerAngles.y, 0f);

        animator.SetTrigger(AttackHash);
    }

    //animation event en el frame del golpe
    public void AttackHit()
    {
        if (!IsAttacking) return;

        Vector3 origin = currentAttackPoint != null ? currentAttackPoint.position : transform.position + transform.forward;
        Collider[] hits = Physics.OverlapSphere(origin, attackRange, ~0, QueryTriggerInteraction.Ignore);
        alreadyHit.Clear();

        bool hitAny = false;
        bool killedAny = false;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null || !alreadyHit.Add(damageable)) continue;

            damageable.TakeDamage(currentDamage);
            hitAny = true;
            if (damageable is Health targetHealth && targetHealth.IsDead) killedAny = true;
            Debug.Log($"HICISTE {currentDamage} DE DAÑO");
        }

        PlaySfx(killedAny ? finalHitClip : hitAny ? hitClip : missClip);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    public void AttackEnd()
    {
        IsAttacking = false;
        if (playerController) playerController.MovementLocked = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        if (weaponVisuals != null)
        {
            foreach (WeaponVisual v in weaponVisuals)
                if (v != null && v.attackPoint != null)
                    Gizmos.DrawWireSphere(v.attackPoint.position, attackRange);
        }

        if (attackPoint != null)
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}