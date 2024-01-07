using UnityEngine;

public class MouseFollow : MonoBehaviour
{
    [SerializeField]
    public Texture2D reticleTexture; // Assign a reticle texture in the Inspector

    [SerializeField]
	public float Sensitivity {
		get { return sensitivity; }
		set { sensitivity = value; }
	}
	[Range(0.1f, 9f)][SerializeField] float sensitivity = 2f;
	[Tooltip("Limits vertical camera rotation. Prevents the flipping that happens when rotation goes above 90.")]
	[Range(0f, 90f)][SerializeField] float yRotationLimit = 88f;

	Vector2 rotation = Vector2.zero;
	const string xAxis = "Mouse X"; //Strings in direct code generate garbage, storing and re-using them creates no garbage
	const string yAxis = "Mouse Y";
    private Transform target; // The target to follow
    public float distance = 5f;
    private float cameraHeight = 3.5f;
    private float cameraZ = 1f;


    void Start()
    {
        // Hide the system cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

	void Update(){
		rotation.x += Input.GetAxis(xAxis) * sensitivity;
		rotation.y += Input.GetAxis(yAxis) * sensitivity;
		rotation.y = Mathf.Clamp(rotation.y, -yRotationLimit, yRotationLimit);
		var xQuat = Quaternion.AngleAxis(rotation.x, Vector3.up);
		var yQuat = Quaternion.AngleAxis(rotation.y, Vector3.left);
        var quatMath = xQuat * yQuat;


        transform.localRotation = quatMath; //Quaternions seem to rotate more consistently than EulerAngles. Sensitivity seemed to change slightly at certain degrees using Euler. transform.localEulerAngles = new Vector3(-rotation.y, rotation.x, 0);

        Quaternion currentRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

        transform.position = target.position + Vector3.up * cameraHeight + Vector3.right * cameraZ;
        transform.position -= currentRotation * Vector3.forward * distance;
    }

    public void SetTarget(Transform targetParam) {
        target = targetParam;
    }
}
