using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
public class NozzleController : MonoBehaviour
{
    [SerializeField] GameObject xAxis;
    [SerializeField] GameObject yAxis;
    [SerializeField] GameObject zAxis;
    [SerializeField] Vector3 nozzleMaxPosition;
    [SerializeField] Vector3 nozzleMinPosition;
    [SerializeField] Vector3 nozzleMinHardwarePosition;
    [SerializeField] Vector3 nozzleMaxHardwarePosition;
    [SerializeField] TMP_Text debugText;

    private void Start()
    {
        SetNozzlePosition(Vector3.zero);
    }

    public void SetNozzlePosition(Vector3 position)
    {
        Vector3 mappedPosition = MapInputToPosition(position);
        xAxis.transform.localPosition = new Vector3(mappedPosition.x, xAxis.transform.localPosition.y, xAxis.transform.localPosition.z);
        yAxis.transform.localPosition = new Vector3(yAxis.transform.localPosition.x, mappedPosition.y, yAxis.transform.localPosition.z);
        zAxis.transform.localPosition = new Vector3(zAxis.transform.localPosition.x, zAxis.transform.localPosition.y, mappedPosition.z);

        // Update the debug text with the new positions
        UpdateDebugText(position);
    }

    private Vector3 MapInputToPosition(Vector3 inputPosition)
    {
        float mappedX = Mathf.Lerp(nozzleMinPosition.x, nozzleMaxPosition.x, Mathf.InverseLerp(nozzleMinHardwarePosition.x, nozzleMaxHardwarePosition.x, inputPosition.x));
        float mappedY = Mathf.Lerp(nozzleMinPosition.y, nozzleMaxPosition.y, Mathf.InverseLerp(nozzleMinHardwarePosition.y, nozzleMaxHardwarePosition.y, inputPosition.y));
        float mappedZ = Mathf.Lerp(nozzleMinPosition.z, nozzleMaxPosition.z, Mathf.InverseLerp(nozzleMinHardwarePosition.z, nozzleMaxHardwarePosition.z, inputPosition.z));
        return new Vector3(mappedX, mappedY, mappedZ);
    }

    private void UpdateDebugText(Vector3 position)
    {
        if (debugText != null)
        {
            debugText.text = $"Nozzle Position - X: {position.x:F2}, Y: {position.z:F2}, Z: {position.y:F2}";
        }
    }
}
