using UnityEngine;
public class KneeController : MonoBehaviour
{
    public HingeJoint kneeJoint;
    public float reflexSpeed = 44f;
    JointSpring jointSpring;
    bool reflexTriggered = false;
    void Start()
    {
        jointSpring = kneeJoint.spring;
        jointSpring.targetPosition = 0f;
        kneeJoint.spring = jointSpring;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X) && !reflexTriggered)
        {
            jointSpring.targetPosition = 50f;
            kneeJoint.spring = jointSpring;

            reflexTriggered = true;
        }

        if (reflexTriggered)
        {

            if (jointSpring.targetPosition > 0f)
            {
                jointSpring.targetPosition = Mathf.Lerp(jointSpring.targetPosition, 0f, reflexSpeed * Time.deltaTime);
                kneeJoint.spring = jointSpring;
            }

            if (0.00001 > jointSpring.targetPosition)
            {
                jointSpring.targetPosition = 0f;
                reflexTriggered = false;
            }
        }
    }
}