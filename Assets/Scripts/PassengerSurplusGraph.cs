using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PassengerSurplusGraph : MonoBehaviour
{
  private RectTransform graphContainer;
  [SerializeField] private LineRenderer lrPrefab;
  [SerializeField] private Transform dotPrefab;
  [SerializeField] private TMP_Text textPrefab;
  [SerializeField] private TMP_Text headerTextPrefab;
  [SerializeField] private TMP_Text legendTextPrefab;



  LineRenderer fourthQuartileUtilitySurplusPerCapitaLine;

  float accumulatedPassengerUtilitySurplus = 0f;

  List<Vector2> passengerSurplusPoints = new List<Vector2>();

  List<PassengerBehavior> pickedUpPassengers = new List<PassengerBehavior>();

  float margin = 26f;
  float marginTop = 50f;
  float maxY = 3f;
  float minY = 0f;
  float maxX = 180f;
  float minX = 0f;

  const float timeInterval = 2f;


  private void Awake()
  {
    graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
    InstantiateGraph();

    StartCoroutine(UpdateGraphAtInterval());
  }

  public void AppendPassenger(PassengerBehavior passenger)
  {
    pickedUpPassengers.Add(passenger);
    accumulatedPassengerUtilitySurplus += passenger.passengerPickedUpData.utilitySurplus;
  }

  IEnumerator UpdateGraphAtInterval()
  {
    while (true)
    {
      yield return new WaitForSeconds(timeInterval);
      UpdateGraph();
    }
  }

  private void UpdateGraph()
  {
    float simulationTime = TimeUtils.ConvertRealSecondsToSimulationHours(Time.time);
    float simulationTimeMinutes = simulationTime * 60f;

    float[] quartiledUtilitySurplusPerCapita = CalculateQuartiledUtilitySurplusPerCapita();

    // Create fourth quartile line if it is null
    if (fourthQuartileUtilitySurplusPerCapitaLine == null)
    {
      InstantiateLine(3);
    }

    Debug.Log("Quartiled utility surplus per capita: " + quartiledUtilitySurplusPerCapita[0] + ", " + quartiledUtilitySurplusPerCapita[1] + ", " + quartiledUtilitySurplusPerCapita[2] + ", " + quartiledUtilitySurplusPerCapita[3]);

    Vector2 fourthQuartilePoint = new Vector2(simulationTimeMinutes, quartiledUtilitySurplusPerCapita[3]);
    passengerSurplusPoints.Add(fourthQuartilePoint);
    fourthQuartileUtilitySurplusPerCapitaLine.positionCount++;
    Vector2 graphPosition = ConvertValueToGraphPosition(fourthQuartilePoint);

    Debug.Log("Point: " + fourthQuartilePoint);
    Debug.Log("Graph position: " + graphPosition);

    fourthQuartileUtilitySurplusPerCapitaLine.SetPosition(fourthQuartileUtilitySurplusPerCapitaLine.positionCount - 1, new Vector3(graphPosition.x, graphPosition.y, 0));

  }

  float[] CalculateQuartiledUtilitySurplusPerCapita()
  {
    float[] quartiledUtilitySurplusPerCapita = new float[4];
    float[] quartiledUtilitySurplus = new float[4];
    int[] quartiledPopulation = new int[4];
    // FIXME: Hard-coded values for now based on mu=0.7 and median 20
    float[] quartiledIncomeTopRange = { 12.47f, 20.0f, 32.07f, float.PositiveInfinity };
    foreach (PassengerBehavior passenger in pickedUpPassengers)
    {
      float utilitySurplus = passenger.passengerPickedUpData.utilitySurplus;
      float hourlyIncome = passenger.passengerEconomicParameters.hourlyIncome;

      if (hourlyIncome < quartiledIncomeTopRange[0])
      {
        quartiledUtilitySurplus[0] += utilitySurplus;
        quartiledPopulation[0]++;
      }
      else if (hourlyIncome < quartiledIncomeTopRange[1])
      {
        quartiledUtilitySurplus[1] += utilitySurplus;
        quartiledPopulation[1]++;
      }
      else if (hourlyIncome < quartiledIncomeTopRange[2])
      {
        quartiledUtilitySurplus[2] += utilitySurplus;
        quartiledPopulation[2]++;
      }
      else
      {
        quartiledUtilitySurplus[3] += utilitySurplus;
        quartiledPopulation[3]++;
      }
    }

    for (int i = 0; i < 4; i++)
    {
      if (quartiledPopulation[i] != 0)
      {
        quartiledUtilitySurplusPerCapita[i] = quartiledUtilitySurplus[i] / quartiledPopulation[i];
      }
    }

    return quartiledUtilitySurplusPerCapita;
  }


  private void CreateAxes()
  {
    // Create x axis with the line renderer
    LineRenderer xLineRenderer = Instantiate(lrPrefab, graphContainer);
    xLineRenderer.positionCount = 2;
    Vector2 zeroPosition = ConvertValueToGraphPosition(new Vector2(0, 0));
    Vector2 maxXPosition = ConvertValueToGraphPosition(new Vector2(maxX, 0));
    xLineRenderer.SetPosition(0, new Vector3(zeroPosition.x, zeroPosition.y, 0));
    xLineRenderer.SetPosition(1, new Vector3(maxXPosition.x, maxXPosition.y, 0));

    // Create y axis with the line renderer
    LineRenderer yLineRenderer = Instantiate(lrPrefab, graphContainer);
    yLineRenderer.positionCount = 2;
    Vector2 maxYPosition = ConvertValueToGraphPosition(new Vector2(0, maxY));
    yLineRenderer.SetPosition(0, new Vector3(zeroPosition.x, zeroPosition.y, 0));
    yLineRenderer.SetPosition(1, new Vector3(maxYPosition.x, maxYPosition.y, 0));

  }

  private void CreateAxisLabels()
  {
    // Create y axis labels
    int step = Mathf.RoundToInt((maxY - minY) / 5f);
    for (int i = (int)minY; i <= maxY; i += step)
    {
      TMP_Text text = Instantiate(textPrefab, graphContainer);
      Vector2 textPosition = ConvertValueToGraphPosition(new Vector2(0, i));
      text.text = i.ToString();
      text.rectTransform.anchoredPosition = textPosition;
    }
  }

  private void CreateHeaderText()
  {
    TMP_Text text = Instantiate(headerTextPrefab, graphContainer);
    Vector2 textPosition = new Vector2(-52f, 70f);
    text.text = "Surplus";
    text.rectTransform.anchoredPosition = textPosition;
  }

  private void CreateLegend()
  {
    TMP_Text text1 = Instantiate(legendTextPrefab, graphContainer);
    Vector2 textPosition1 = new Vector2(80, 74f);
    text1.text = "Acc surplus";
    text1.rectTransform.anchoredPosition = textPosition1;

    // Create a tiny green line with the line renderer
    LineRenderer greenLine = Instantiate(lrPrefab, graphContainer);
    greenLine.positionCount = 2;
    Vector2 greenLinePosition1 = new Vector2(225, 181);
    Vector2 greenLinePosition2 = new Vector2(235, 181);
    greenLine.SetPosition(0, new Vector3(greenLinePosition1.x, greenLinePosition1.y, 0));
    greenLine.SetPosition(1, new Vector3(greenLinePosition2.x, greenLinePosition2.y, 0));
    greenLine.startColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
    greenLine.endColor = new Color(0.0f, 1.0f, 0.0f, 1.0f);
  }

  private void InstantiateLine(int quartile)
  {
    LineRenderer line = Instantiate(lrPrefab, graphContainer);
    line.positionCount = 0;

    if (quartile == 3)
    {
      // Set line to purple color
      line.startColor = new Color(0.5f, 0.0f, 0.5f, 1.0f);
      line.endColor = new Color(0.5f, 0.0f, 0.5f, 1.0f);
      fourthQuartileUtilitySurplusPerCapitaLine = line;
    }
  }

  private void InstantiateGraph()
  {
    CreateAxes();
    CreateAxisLabels();
    CreateHeaderText();
    CreateLegend();
  }


  private Vector2 ConvertValueToGraphPosition(Vector2 vector)
  {
    float graphHeight = graphContainer.sizeDelta.y;
    float graphWidth = graphContainer.sizeDelta.x;

    float y = Mathf.Lerp(margin, graphHeight - marginTop, (vector.y - minY) / (maxY - minY));
    float x = Mathf.Lerp(margin, graphWidth - margin, (vector.x - minX) / (maxX - minX));

    return new Vector2(x, y);
  }


  private void CreateDot(Vector2 position)
  {
    Transform dot = Instantiate(dotPrefab, graphContainer);
    RectTransform rectTransform = dot.GetComponent<RectTransform>();
    rectTransform.anchorMin = new Vector2(0, 0);
    rectTransform.anchorMax = new Vector2(0, 0);
    rectTransform.anchoredPosition = new Vector3(position.x, position.y, -1);

  }
}

