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

  List<LineRenderer> quartileLines = new List<LineRenderer>(4);

  TMP_Text firstQuartileLegend;
  TMP_Text secondQuartileLegend;
  TMP_Text thirdQuartileLegend;
  TMP_Text fourthQuartileLegend;



  List<Vector2> passengerSurplusPoints = new List<Vector2>();

  List<Passenger> passengers = new List<Passenger>();

  float margin = 26f;
  float marginTop = 50f;
  float maxY = 3f;
  float minY = 0f;
  float maxX = 4; // TODO: SimulationSettings.simulationLengthHours;
  float minX = 0f;
  Color[] quartileColors = { new Color(1.0f, 1.0f, 0.0f, 1.0f), new Color(0.0f, 1.0f, 0.0f, 1.0f), new Color(0.0f, 0.0f, 1.0f, 1.0f), new Color(0.5f, 0.0f, 0.5f, 1.0f) };

  const float timeInterval = 2f;


  private void Awake()
  {
    graphContainer = transform.Find("GraphContainer").GetComponent<RectTransform>();
    InstantiateGraph();

    StartCoroutine(UpdateGraphAtInterval());
  }

  public void AppendPassenger(Passenger passenger)
  {
    passengers.Add(passenger);
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

    (float[] quartiledUtilitySurplusPerCapita, int[] quartiledPopulation) = CalculateQuartiledUtilitySurplusPerCapita();

    UpdateLegends(quartiledPopulation);
    // Debug.Log("Quartiled utility surplus per capita: " + quartiledUtilitySurplusPerCapita[0] + ", " + quartiledUtilitySurplusPerCapita[1] + ", " + quartiledUtilitySurplusPerCapita[2] + ", " + quartiledUtilitySurplusPerCapita[3]);

    // Add points to all 4 quartile lines
    for (int i = 0; i < 4; i++)
    {
      if (quartiledPopulation[i] == 0)
      {
        continue;
      }
      LineRenderer quartileLine = quartileLines[i];
      Vector2 quartilePoint = new Vector2(simulationTime, quartiledUtilitySurplusPerCapita[i]);
      // passengerSurplusPoints.Add(quartilePoint);
      quartileLine.positionCount++;
      Vector2 graphPosition = ConvertValueToGraphPosition(quartilePoint);
      quartileLine.SetPosition(quartileLine.positionCount - 1, new Vector3(graphPosition.x, graphPosition.y, 0));
    }

  }

  (float[] quartiledUtilitySurplusPerCapita, int[] quartiledPopulation) CalculateQuartiledUtilitySurplusPerCapita()
  {
    float[] quartiledUtilitySurplusPerCapita = new float[4];
    float[] quartiledUtilitySurplus = new float[4];
    int[] quartiledPopulation = new int[4];
    // FIXME: Hard-coded values for now based on mu=0.9, median 16 + 4 fixed income
    float[] quartiledIncomeTopRange = { 12.72f, 20.0f, 33.36f, float.PositiveInfinity };
    foreach (Passenger passenger in passengers)
    {
      float utilitySurplus = passenger.currentTrip != null ? passenger.currentTrip.droppedOffPassengerData.utilitySurplus : 0;
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

    return (quartiledUtilitySurplusPerCapita, quartiledPopulation);
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

  private void CreateAxisValues()
  {
    // Create y axis values
    int step = Mathf.RoundToInt((maxY - minY) / 5f);
    for (int i = (int)minY; i <= maxY; i += step)
    {
      TMP_Text text = Instantiate(textPrefab, graphContainer);
      Vector2 textPosition = ConvertValueToGraphPosition(new Vector2(0, i));
      text.text = i.ToString();
      text.rectTransform.anchoredPosition = textPosition;
    }

    // Create x axis values
    step = Mathf.RoundToInt((maxX - minX) / 6f);
    for (int i = (int)minX; i <= maxX; i += step)
    {
      TMP_Text text = Instantiate(textPrefab, graphContainer);
      Vector2 textPosition = ConvertValueToGraphPosition(new Vector2(i, 0));
      // Set pivot to top center
      text.rectTransform.pivot = new Vector2(0.5f, 1f);
      // Set textmeshpro text alignment to center
      text.alignment = TextAlignmentOptions.Center;
      text.text = TimeUtils.ConvertSimulationHoursToTimeString(i);
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
    Vector2 textPosition1 = new Vector2(120f, 74f);
    Vector2 textPosition2 = new Vector2(120f, 60f);
    Vector2 textPosition3 = new Vector2(120f, 46f);
    Vector2 textPosition4 = new Vector2(120f, 32f);
    Vector2[] textPositions = { textPosition1, textPosition2, textPosition3, textPosition4 };

    for (int i = 0; i < 4; i++)
    {
      TMP_Text text = Instantiate(legendTextPrefab, graphContainer);
      text.rectTransform.anchoredPosition = textPositions[i];
      text.rectTransform.sizeDelta = new Vector2(160, 30);

      // Create a tiny green line with the line renderer
      LineRenderer line = Instantiate(lrPrefab, graphContainer);

      RectTransform lineRectTransform = line.GetComponent<RectTransform>();
      lineRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
      lineRectTransform.anchorMax = new Vector2(0.5f, 0.5f);

      line.positionCount = 2;
      Vector2 linePos1 = textPositions[i] + new Vector2(-95, 7);
      Vector2 linePos2 = textPositions[i] + new Vector2(-85, 7);
      line.SetPosition(0, new Vector3(linePos1.x, linePos1.y, 0));
      line.SetPosition(1, new Vector3(linePos2.x, linePos2.y, 0));
      line.startColor = quartileColors[i];
      line.endColor = quartileColors[i];


      if (i == 0)
      {
        firstQuartileLegend = text;
      }
      else if (i == 1)
      {
        secondQuartileLegend = text;
      }
      else if (i == 2)
      {
        thirdQuartileLegend = text;
      }
      else
      {
        fourthQuartileLegend = text;
      }
    }

    UpdateLegends(new int[] { 0, 0, 0, 0 });
  }


  private void UpdateLegends(int[] quartiledPopulation)
  {
    firstQuartileLegend.text = "First quartile (n=" + quartiledPopulation[0] + ")";
    secondQuartileLegend.text = "Second quartile (n=" + quartiledPopulation[1] + ")";
    thirdQuartileLegend.text = "Third quartile (n=" + quartiledPopulation[2] + ")";
    fourthQuartileLegend.text = "Fourth quartile (n=" + quartiledPopulation[3] + ")";
  }

  private void InstantiateLines()
  {


    for (int i = 0; i < 4; i++)
    {
      LineRenderer line = Instantiate(lrPrefab, graphContainer);
      line.positionCount = 0;
      line.startColor = quartileColors[i];
      line.endColor = quartileColors[i];
      quartileLines.Add(line);
    }
  }

  private void InstantiateGraph()
  {
    CreateAxes();
    CreateAxisValues();
    CreateHeaderText();
    CreateLegend();
    InstantiateLines();
  }


  private Vector2 ConvertValueToGraphPosition(Vector2 vector)
  {
    float graphHeight = graphContainer.sizeDelta.y;
    float graphWidth = graphContainer.sizeDelta.x;

    float y = Mathf.Lerp(margin, graphHeight - marginTop, (vector.y - minY) / (maxY - minY));
    float x = Mathf.Lerp(margin, graphWidth - margin, (vector.x - minX) / (maxX - minX));

    return new Vector2(x, y);
  }

}

