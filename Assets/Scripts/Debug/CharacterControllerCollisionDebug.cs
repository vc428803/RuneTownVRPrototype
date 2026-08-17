using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

public class CharacterControllerCollisionDebug : MonoBehaviour
{
    private XRBodyTransformer bodyTransformer;

    private void Start()
    {
        // 修改原因：確認 XR Origin 上是否存在多個 XRBodyTransformer，
        // 導致 LocomotionMediator 與我們檢查的 Transformer 不是同一個 Instance。
        var transformers =
            GetComponents<XRBodyTransformer>();

        var mediator =
            GetComponent<LocomotionMediator>();

        UnityEngine.Debug.Log(
            "[XR Transformer Count Debug] Count=" +
            transformers.Length
        );

        for (int i = 0; i < transformers.Length; i++)
        {
            var transformer = transformers[i];

            string manipulatorName = "NULL";

            if (transformformerHasManipulator(transformer))
            {
                manipulatorName =
                    transformer.constrainedBodyManipulator.GetType().FullName;
            }

            UnityEngine.Debug.Log(
                "[XR Transformer Count Debug] #" + i +
                ", InstanceID=" + transformer.GetInstanceID() +
                ", Manipulator=" + manipulatorName
            );
        }

        if (mediator != null && mediator.bodyTransformer != null)
        {
            UnityEngine.Debug.Log(
                "[XR Mediator Debug] BodyTransformer InstanceID=" +
                mediator.bodyTransformer.GetInstanceID()
            );
        }

        // 修改原因：監聽目前 XRBodyTransformer 的 transformation Apply，
        // 確認 Continuous Move Provider 是否真的有把移動送進來。
        bodyTransformer = GetComponent<XRBodyTransformer>();

        if (bodyTransformer != null)
        {
            bodyTransformer.beforeApplyTransformations +=
                OnBeforeApplyTransformations;

            bodyTransformer.afterApplyTransformations +=
                OnAfterApplyTransformations;

            UnityEngine.Debug.Log(
                "[XR Transformation Debug] Event listeners registered."
            );
        }
        else
        {
            UnityEngine.Debug.LogWarning(
                "[XR Transformation Debug] XRBodyTransformer not found."
            );
        }
    }

    private bool transformformerHasManipulator(
        XRBodyTransformer transformer
    )
    {
        return transformer != null &&
               transformer.constrainedBodyManipulator != null;
    }

    private void OnBeforeApplyTransformations(
        XRBodyTransformer transformer
    )
    {
        // 修改原因：確認 XRBodyTransformer 是否開始處理
        // Continuous Move Provider 排入的 transformation。
        UnityEngine.Debug.Log(
            "[XR Transformation Debug] BEFORE APPLY"
        );
    }

    private void OnAfterApplyTransformations(
        ApplyBodyTransformationsEventArgs args
    )
    {
        // 修改原因：確認 XRBodyTransformer 是否完成
        // transformation Apply。
        UnityEngine.Debug.Log(
            "[XR Transformation Debug] AFTER APPLY"
        );
    }

    private void OnControllerColliderHit(
        ControllerColliderHit hit
    )
    {
        // 修改原因：確認 XR CharacterController 是否真的碰到
        // GAT WalkMap MeshCollider，排除 Layer Matrix 正常但實際未碰撞的情況。
        UnityEngine.Debug.Log(
            "[CharacterController Collision Debug] " +
            "Hit=" + hit.gameObject.name +
            ", Layer=" + LayerMask.LayerToName(hit.gameObject.layer) +
            ", Point=" + hit.point +
            ", Normal=" + hit.normal
        );
    }

    private void OnDestroy()
    {
        // 修改原因：避免物件銷毀後仍保留 XRBodyTransformer 事件訂閱。
        if (bodyTransformer != null)
        {
            bodyTransformer.beforeApplyTransformations -=
                OnBeforeApplyTransformations;

            bodyTransformer.afterApplyTransformations -=
                OnAfterApplyTransformations;
        }
    }
}