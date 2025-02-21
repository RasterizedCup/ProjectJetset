using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;


public class PlayerDash : PlayerMovementEffector
{
    [SerializeField]
    float baseDashPower;
    [SerializeField]
    float dashDuration;

    [SerializeField]
    float afterImageSpawnNumber = 5;
    [SerializeField]
    float afterImageLifeDuration = .02f;
    [SerializeField]
    float afterImageVerticalOffsetFix = -1.35f;
    [SerializeField]
    Material afterImageMaterial;
    [SerializeField]
    GameObject parentObj;
    [SerializeField]
    [Range(0.001f, 1)]
    float meshRebakeFrequency;

    [SerializeField]
    Material mat;
    [SerializeField]
    string shaderVarRef;
    [SerializeField]
    float shaderVarRate = .1f;
    [SerializeField]
    float shaderVarRefreshRate = 0.05f;

    private SkinnedMeshRenderer afterImage;

    bool isDashAttempt = false;
    bool isDashing = false;
    float currTime = 0;
    float currAfterImageTime = 0;
    float currMeshRebakeTime = 0;
    Vector3 DashDirection = Vector3.zero;
    private int currJumpGraceFrame = 0;
    private int jumpInputGraceFrames = 3;
    private float afterImageSpawnFrequency = .2f;
    private float meshRebakeFreq;

    List<GameObject> afterImageObjects;

    float timeRemainingInDash = 0;
    // Start is called before the first frame update
    void Start()
    {
        afterImageSpawnFrequency = dashDuration / afterImageSpawnNumber;
        meshRebakeFreq = dashDuration / (afterImageSpawnNumber * meshRebakeFrequency);
        afterImageObjects = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        checkIfDashInput();
    }

    void checkIfDashInput()
    {

        if (Input.GetKeyDown(KeyCode.Joystick1Button8) || Input.GetKeyDown(KeyCode.LeftShift))
        {
            isDashAttempt = true;
        } // sometimes input is overlooked. allow a brief grace period to compensate
        else if (currJumpGraceFrame > jumpInputGraceFrames && isDashAttempt)
        {
            currJumpGraceFrame = 0;
            isDashAttempt = false;
        }
        else if (isDashAttempt)
        {
            currJumpGraceFrame++;
        }
    }

    Vector3 CameraRelativeFlatten(Vector3 input, Vector3 localUp, bool camIsRight = true)
    {
        // pass horiz and vertical to set input ratios

        Transform camera = cam.transform; // You can cache this to save a search.
        Quaternion flatten = Quaternion.LookRotation(-localUp, (camIsRight) ? camera.forward : camera.right) * Quaternion.Euler(-90f, 0, 0);

        return flatten * input;
    }

    // if no input, default to playerForward
    void handleSetDashDirection()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        DashDirection = new Vector3(horizontal, 0f, vertical).normalized;
        // default the dash to forward if no direction input
        if(DashDirection.magnitude == 0)
        {
            DashDirection = Vector3.forward;
        }
        DashDirection = CameraRelativeFlatten(DashDirection, Vector3.up);
    }

    void handleDashAfterimage()
    {
        if(Time.time > currAfterImageTime)
        {
            currAfterImageTime = Time.time + afterImageSpawnFrequency;
            if (afterImage == null)
            {
                afterImage = gfx.GetComponentInChildren<SkinnedMeshRenderer>();
            }
            GameObject afterIm = new GameObject();
            Vector3 updatedPosition = controller.transform.position;
            updatedPosition.y -= 1.35f;
            afterIm.transform.position = updatedPosition;
            Vector3 updatedRotation = Vector3.zero;
            updatedRotation.y = controller.transform.rotation.eulerAngles.y;
            updatedRotation.x = afterImage.transform.rotation.eulerAngles.x;
            afterIm.transform.rotation = Quaternion.Euler(updatedRotation.x, updatedRotation.y, updatedRotation.z);
            MeshRenderer mr = afterIm.AddComponent<MeshRenderer>();
            MeshFilter mf = afterIm.AddComponent<MeshFilter>();

            Mesh mesh = new Mesh();
            afterImage.BakeMesh(mesh);
            mr.material = afterImageMaterial;
            mf.mesh = mesh;
            Destroy(afterIm, afterImageLifeDuration);
        }
    }

    IEnumerator handleAfterImage(float timeActive)
    {
        Mesh mesh = new Mesh();
        afterImageSpawnFrequency = dashDuration / afterImageSpawnNumber;
        meshRebakeFreq = dashDuration / (afterImageSpawnNumber * meshRebakeFrequency);
        while (timeActive > 0 && !RailDetect.isOnSmoothRail && !AttachToWall.isAttachedToWall
            && !AttachToRail.isAttachedToRail)
        {
            timeActive -= (afterImageSpawnFrequency + (.002f));

            if (afterImage == null)
            {
                afterImage = gfx.GetComponentInChildren<SkinnedMeshRenderer>();
            }
            GameObject afterIm = new GameObject();
            Vector3 updatedPosition = controller.transform.position;
            updatedPosition.y += afterImageVerticalOffsetFix;
            afterIm.transform.position = updatedPosition;
            Vector3 updatedRotation = Vector3.zero;
            updatedRotation.y = controller.transform.rotation.eulerAngles.y;
            updatedRotation.x = afterImage.transform.rotation.eulerAngles.x;
            afterIm.transform.rotation = Quaternion.Euler(updatedRotation.x, updatedRotation.y, updatedRotation.z);
            MeshRenderer mr = afterIm.AddComponent<MeshRenderer>();
            MeshFilter mf = afterIm.AddComponent<MeshFilter>();

            // only rebake the mesh per freq update (1 is 1:1, 0 is no rebake)
            if (Time.time > currMeshRebakeTime)
            {
                afterImage.BakeMesh(mesh);
                currMeshRebakeTime = Time.time + meshRebakeFreq;
            }

            mr.material = afterImageMaterial;
            mf.mesh = mesh;
            StartCoroutine(AnimateMaterialFloat(mr.material, 0, shaderVarRate, shaderVarRefreshRate));

            Destroy(afterIm, afterImageLifeDuration);

            yield return new WaitForSeconds(afterImageSpawnFrequency);
        }
        currMeshRebakeTime = 0;
    }

    IEnumerator AnimateMaterialFloat(Material mat, float goal, float rate, float refreshRate)
    {
        float valueToAnimate = mat.GetFloat(shaderVarRef);
        while(valueToAnimate > goal)
        {
            valueToAnimate -= rate;
            mat.SetFloat(shaderVarRef, valueToAnimate);
            yield return new WaitForSeconds(refreshRate);
        }
    }

    public Vector3 HandlePlayerDash(ref PlayerMovementContext moveContext)
    {
        // consolidate this mess lmao (add more of these as functions to move context?)
        if(isDashAttempt && !moveContext.isGrounded && !RailDetect.isOnSmoothRail && !AttachToWall.isAttachedToWall 
            && !AttachToRail.isAttachedToRail && !isDashing)
        {
            afterImageSpawnFrequency = dashDuration / afterImageSpawnNumber;
            isDashAttempt = false;
            isDashing = true;
            moveContext.isDashing = true;
            currTime = Time.time + dashDuration;
            handleSetDashDirection();
            timeRemainingInDash = dashDuration;
            StartCoroutine(handleAfterImage(timeRemainingInDash));

        }
        if (isDashing && currTime > Time.time)
        {
            return new Vector3(
                DashDirection.x * baseDashPower,
                DashDirection.y * baseDashPower,
                DashDirection.z * baseDashPower);
        }
        else
        {
            isDashing = false;
            moveContext.isDashing = false;
            return Vector3.zero;
        }
    }
}
