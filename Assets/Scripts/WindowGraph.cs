using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeMonkey;
using CodeMonkey.Utils;
using Random = System.Random;

public class WindowGraph : MonoBehaviour
{
    // Declaration of GUI elements.
    public RectTransform GraphContainer;
    public RectTransform LabelTemplateX;
    public RectTransform LabelTemplateY;
    public RectTransform DashTemplateX;
    public RectTransform DashTemplateY;
    public List<GameObject> GameObjectList;
    public List<IGraphVisualObject> GraphVisualObjectList;
    [SerializeField] public Sprite DotSprite;  // Use to display each dot on the graph.
    public List<RectTransform> yLabelList;

    // Cached values.
    public float xSize;
    public List<int> valueList;
    public bool startYScaleAtZero;
    public int maxVisibleValueAmount;
    public IGraphVisual GraphVisual;
    public Func<int, string> getAxisLabelX;
    public Func<float, string> getAxisLabelY;


    public WindowGraph(RectTransform graphContainer, IGraphVisual graphVisual)
    {
        GameObjectList = new List<GameObject>();
        GraphVisualObjectList = new List<IGraphVisualObject>();
        startYScaleAtZero = true;
        yLabelList = new List<RectTransform>();
        GraphContainer = graphContainer;
        LabelTemplateX = GraphContainer.Find("LabelTemplateX").GetComponent<RectTransform>();
        LabelTemplateY = GraphContainer.Find("LabelTemplateY").GetComponent<RectTransform>();
        DashTemplateX = GraphContainer.Find("DashTemplateY").GetComponent<RectTransform>();
        DashTemplateY = GraphContainer.Find("DashTemplateX").GetComponent<RectTransform>();
        GraphVisual = graphVisual;
    }

    void Awake()
    {
        Console.WriteLine("AWAKE WINDOWGRAPH!!!");
    }

    // Function that manages the graph generation procedure (receiving a list of coordinates for our points.
    public void ShowGraph(List<int> valueList, IGraphVisual graphVisual, int maxVisibleValueAmount = -1, Func<int, string> getAxisLabelX = null, Func<float, string> getAxisLabelY = null)
    {
        // Replace local reference to graphVisual by our global variable when the input is null.
        if (graphVisual == null)
        {
            graphVisual = this.GraphVisual;
        }

        // Update member variables.
        this.valueList = valueList;
        this.GraphVisual = graphVisual;
        this.getAxisLabelX = getAxisLabelX;
        this.getAxisLabelY = getAxisLabelY;

        // Deal with null optional inputs.
        if (getAxisLabelX == null)
        {
            getAxisLabelX = delegate(int _i) { return _i.ToString();};
        }
        if (getAxisLabelY == null)
        {
            getAxisLabelY = delegate (float _f) { return Mathf.RoundToInt(_f).ToString(); };
        }

        // Check if maxVisibleValueAmount is valid.
        if (maxVisibleValueAmount <= 0)
        {
            maxVisibleValueAmount = valueList.Count;
        }
        this.maxVisibleValueAmount = maxVisibleValueAmount;

        // Destroy last objects in the plotting zone.
        foreach (GameObject gameObject in GameObjectList)
        {
            Destroy(gameObject);
        }
        GameObjectList.Clear();
        yLabelList.Clear();

        // Definition of variables.
        float graphHeight = GraphContainer.sizeDelta.y;
        float graphWidth = GraphContainer.sizeDelta.x;

        float yMinimum, yMaximum;
        CalculateYScale(out yMinimum, out yMaximum);
        xSize = graphWidth / (maxVisibleValueAmount + 1); // Distance between consecutive points on xAxis of our graph (resolution).
        
        // Iterate along the list (for accessing each point coordinates). 
        int xIndex = 0;
        for (int i = Mathf.Max(valueList.Count - maxVisibleValueAmount, 0); i < valueList.Count; i++)
        {
            // Conversion of sequence number to a x coordinate accordingly to our graph properties.
            float xPosition = xIndex * xSize;

            // Conversion of value inside value list to a y coordinate accordingly to our graph properties.
            float yPosition = ((valueList[i] - yMinimum) / (yMaximum - yMinimum)) * graphHeight; // Normalization to our graph maximum height.

            // Create Line Graph Visual Object.
            GraphVisualObjectList.Add(graphVisual.CreateGraphVisualObject(new Vector2(xPosition, yPosition), xSize));

            // Create x axis separators.
            RectTransform labelX = Instantiate(LabelTemplateX);
            labelX.SetParent(GraphContainer, false);
            labelX.gameObject.SetActive(true); // Make template label visible.
            labelX.anchoredPosition = new Vector2(xPosition, -0.5f); // Label position.
            labelX.GetComponent<Text>().text = getAxisLabelX(i);  // Define the text of the label.
            GameObjectList.Add(labelX.gameObject);

            // Create x axis dashes.
            RectTransform dashX = Instantiate(DashTemplateX);
            dashX.SetParent(GraphContainer, false);
            dashX.gameObject.SetActive(true); // Make template label visible.
            dashX.anchoredPosition = new Vector2(xPosition, -0.5f); // Label position.
            GameObjectList.Add(dashX.gameObject);

            // Update counter.
            xIndex++;
        }

        // Create y axis separators.
        int separatorCount = 10;
        for (int i = 0; i <= separatorCount; i++)
        {
            RectTransform labelY = Instantiate(LabelTemplateY);
            labelY.SetParent(GraphContainer, false);
            labelY.gameObject.SetActive(true); // Make template label visible.
            float normalizedValue = i * 1f / separatorCount;
            labelY.anchoredPosition = new Vector2(-0.5f, normalizedValue * graphHeight); // Label position.
            labelY.GetComponent<Text>().text = getAxisLabelY(yMinimum + (normalizedValue * (yMaximum - yMinimum)));  // Define the text of the label.
            yLabelList.Add(labelY);
            GameObjectList.Add(labelY.gameObject);

            // Create x axis dashes.
            RectTransform dashY = Instantiate(DashTemplateY);
            dashY.SetParent(GraphContainer, false);
            dashY.gameObject.SetActive(true); // Make template label visible.
            dashY.anchoredPosition = new Vector2(-0.5f, normalizedValue * graphHeight); // Label position.
            GameObjectList.Add(dashY.gameObject);
        }
    }

    // Update a single value of our plot.
    public void UpdateValue(List<int> newValues)
    {
        // Definition of variables.
        float graphHeight = GraphContainer.sizeDelta.y;
        float graphWidth = GraphContainer.sizeDelta.x;
        float yMinimumBefore, yMaximumBefore;
        CalculateYScale(out yMinimumBefore, out yMaximumBefore);

        // Shift array.
        valueList = valueList.GetRange(newValues.Count, valueList.Count - newValues.Count);
        valueList.AddRange(newValues);

        // After update.
        float yMinimum, yMaximum;
        CalculateYScale(out yMinimum, out yMaximum);

        // Iterate along the list (for accessing each point coordinates). 
        int xIndex = 0;
        for (int i = Mathf.Max(valueList.Count - maxVisibleValueAmount, 0); i < valueList.Count; i++)
        {
            // Conversion of sequence number to a x coordinate accordingly to our graph properties.
            float xPosition = xIndex * xSize;

            // Conversion of value inside value list to a y coordinate accordingly to our graph properties.
            float yPosition = ((valueList[i] - yMinimum) / (yMaximum - yMinimum)) * graphHeight; // Normalization to our graph maximum height.

            // Create Line Graph Visual Object.
            GraphVisualObjectList[xIndex].SetGraphicalVisualObjectInfo(new Vector2(xPosition, yPosition), xSize);

            // Update counter.
            xIndex++;
        }

        for (int i = 0; i < yLabelList.Count; i++)
        {
            float normalizedValue = i * 1f / yLabelList.Count;
            yLabelList[i].GetComponent<Text>().text = getAxisLabelY(yMinimum + (normalizedValue * (yMaximum - yMinimum)));  // Define the text of the label.
        }
    }

    // Auxiliary function that helps in the updating of Y scale in real-time.
    public void CalculateYScale(out float yMinimum, out float yMaximum)  // The "out" element ensures that we can return more than one value from our function.
    {
        // Dynamically update y axis.
        yMaximum = valueList[0]; // Definition of the value in terms of coordinate distances that corresponds to the maximum value inside valueList.
        yMinimum = valueList[0]; // Definition of the value in terms of coordinate distances that corresponds to the maximum value inside valueList.
        for (int i = Mathf.Max(valueList.Count - maxVisibleValueAmount, 0); i < valueList.Count; i++)
        {
            int value = valueList[i];
            // [Maximum]
            if (value > yMaximum) // Update yMaximum.
            {
                yMaximum = value;
            }

            // [Minimum]
            if (value < yMinimum) // Update yMaximum.
            {
                yMinimum = value;
            }
        }

        // Give a margin.
        float yDifference = yMaximum - yMinimum;
        if (yDifference <= 0)
        {
            yDifference = 5f;
        }
        yMaximum = yMaximum + ((yDifference) * 0.2f);
        yMinimum = yMinimum - ((yDifference) * 0.2f);

        if (startYScaleAtZero)
        {
            yMinimum = 0f; // Start the graph at zero.
        }
    }

    // Create a graphical interface.
    public interface IGraphVisual
    {
        IGraphVisualObject CreateGraphVisualObject(Vector2 graphPosition, float graphPositionWidth);
        RectTransform GetGraphContainer();
    }

    // Interface for dealing with individual graphical objects (every dot, connection line... will be a IGraphVisualObject).
    public interface IGraphVisualObject
    {
        void SetGraphicalVisualObjectInfo(Vector2 graphPosition, float graphPositionWidth);
        void CleanUp(); // Clean Visual objects from our interface (in real-time we want to delete older objects).
    }

    // Generic Interface (subclass) dedicated to manage the creation of a Line Graph.
    public class LineGraphVisual : IGraphVisual
    {
        public RectTransform graphContainer;
        public Sprite dotSprite;
        public LineGraphVisualObject lastLineGraphVisualObject; // Reference to the last circle, ensuring the graphical representation of a connection line.
        public Color dotColor;
        public Color dotConnectionColor;

        public LineGraphVisual(RectTransform graphContainer, Sprite dotSprite, Color dotColor, Color dotConnectionColor)
        {
            this.graphContainer = graphContainer;
            this.dotSprite = dotSprite;
            this.lastLineGraphVisualObject = null;
            this.dotColor = dotColor;
            this.dotConnectionColor = dotConnectionColor;
        }

        // Method that retrieves the graphContainer handler.
        public RectTransform GetGraphContainer()
        {
            return this.graphContainer;
        }

        // Method responsible for adding a new visual object (dot, line...).
        public IGraphVisualObject CreateGraphVisualObject(Vector2 graphPosition, float graphPositionWidth)
        {
            // Create a GameObject list to store all the created graphical elements.
            List<GameObject> GameObjectList = new List<GameObject>();

            // Create the circle on the determined coordinates.
            GameObject dotGameObject = CreateDot(graphPosition);
            GameObjectList.Add(dotGameObject);

            // Store the reference for the next iteration.
            GameObject dotConnectionGameObject = null;
            if (lastLineGraphVisualObject != null)
            {
                dotConnectionGameObject = CreateDotConnection(lastLineGraphVisualObject.GetGraphPosition(), dotGameObject.GetComponent<RectTransform>().anchoredPosition);
                GameObjectList.Add(dotConnectionGameObject);
            }

            LineGraphVisualObject lineGraphVisualObject = new LineGraphVisualObject(dotGameObject, dotConnectionGameObject, lastLineGraphVisualObject);
            lineGraphVisualObject.SetGraphicalVisualObjectInfo(graphPosition, graphPositionWidth);

            lastLineGraphVisualObject = lineGraphVisualObject;

            return lineGraphVisualObject;
        }

        // Function dedicated to create a circle for each dot on our plot.
        // The input will be a 2D coordinates of our point.
        public GameObject CreateDot(Vector2 anchoredPosition)
        {
            // Creation of a new game object.
            GameObject gameObject = new GameObject("dot", typeof(Image));

            // Specification of the parent object of our circle.
            gameObject.transform.SetParent(this.graphContainer, false);

            // Definition of the sprite of our game object.
            gameObject.GetComponent<Image>().sprite = this.dotSprite;

            // Change circle color.
            gameObject.GetComponent<Image>().color = dotColor;

            // A reference to RectTransform object: "It's used to store and manipulate the position, size, and anchoring of a rectangle".
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = anchoredPosition; // Definition of the position (it will be the one specified on our input coordinates.
            rectTransform.sizeDelta = new Vector2(11, 11); // Size of our circles.
            rectTransform.anchorMin = new Vector2(0, 0); // Align circle to the lower left corner.
            rectTransform.anchorMax = new Vector2(0, 0);

            // Return statement.
            return gameObject;
        }

        // Function dedicated to the creation of the lines that connect each pair of consecutive points.
        public GameObject CreateDotConnection(Vector2 dotPositionA, Vector2 dotPositionB)
        {
            // Creation of a new game object (rectangle/line).
            GameObject gameObject = new GameObject("dotConnection", typeof(Image));

            // Specification of the parent object of our line.
            gameObject.transform.SetParent(this.graphContainer, false);

            // Change line color.
            gameObject.GetComponent<Image>().color = dotConnectionColor;

            // A reference to RectTransform object: "It's used to store and manipulate the position, size, and anchoring of a rectangle".
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            Vector2 direction = (dotPositionB - dotPositionA).normalized; // Line Orientation (connection between the two points).
            float distance = Vector2.Distance(dotPositionA, dotPositionB); // Euclidean distance between the two connection points.
            rectTransform.anchoredPosition = dotPositionA + direction * distance * 0.5f; // Definition of the position (it will be the one specified on our input coordinates.
            rectTransform.sizeDelta = new Vector2(distance, 3f); // Size of our lines.
            rectTransform.anchorMin = new Vector2(0, 0); // Align circle to the lower left corner.
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.localEulerAngles = new Vector3(0, 0, UtilsClass.GetAngleFromVectorFloat(direction)); // Rotate connection line.

            return gameObject;
        }

        // Subclass managing LineGraphVisual objects (Essential for real-time representation).
        public class LineGraphVisualObject : IGraphVisualObject
        {
            // Events.
            public event EventHandler OnChangedGraphVisualObjectInfo;

            // Member variables.
            private GameObject dotGameObject;
            private GameObject dotConnectionGameObject;
            private LineGraphVisualObject lastVisualObject;

            public LineGraphVisualObject(GameObject dotGameObject, GameObject dotConnectionGameObject, LineGraphVisualObject lastVisualObject)
            {
                this.dotGameObject = dotGameObject;
                this.dotConnectionGameObject = dotConnectionGameObject;
                this.lastVisualObject = lastVisualObject;

                if (lastVisualObject != null)
                {
                    lastVisualObject.OnChangedGraphVisualObjectInfo += LastVisualObject_OnChangedGraphVisualObjectInfo;
                }
            }

            public void LastVisualObject_OnChangedGraphVisualObjectInfo(object sender, EventArgs e)
            {
                UpdateDotConnection();
            }

            // Method that ensures a quick and easy update of our graphical container.
            public void SetGraphicalVisualObjectInfo(Vector2 graphPosition, float graphPositionWidth)
            {
                RectTransform rectTransform = dotGameObject.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = graphPosition;

                // Update dot connection.
                UpdateDotConnection();

                if (OnChangedGraphVisualObjectInfo != null) OnChangedGraphVisualObjectInfo(this, EventArgs.Empty);
            }

            // Method responsible for cleaning each game object.
            public void CleanUp()
            {
                Destroy(dotGameObject);
                Destroy(dotConnectionGameObject);
            }

            // Get graph position.
            public Vector2 GetGraphPosition()
            {
                RectTransform rectTransform = dotGameObject.GetComponent<RectTransform>();
                return rectTransform.anchoredPosition;
            }

            // Update each data connection.
            public void UpdateDotConnection()
            {
                if (dotConnectionGameObject != null)
                {
                    // A reference to RectTransform object: "It's used to store and manipulate the position, size, and anchoring of a rectangle".
                    RectTransform dotConnectionRectTransform = dotConnectionGameObject.GetComponent<RectTransform>();
                    Vector2 direction = (lastVisualObject.GetGraphPosition() - GetGraphPosition()).normalized; // Line Orientation (connection between the two points).
                    float distance = Vector2.Distance(GetGraphPosition(), lastVisualObject.GetGraphPosition()); // Euclidean distance between the two connection points.
                    dotConnectionRectTransform.anchoredPosition = GetGraphPosition() + direction * distance * 0.5f; // Definition of the position (it will be the one specified on our input coordinates.
                    dotConnectionRectTransform.sizeDelta = new Vector2(distance, 3f); // Size of our lines.
                    dotConnectionRectTransform.localEulerAngles = new Vector3(0, 0, UtilsClass.GetAngleFromVectorFloat(direction)); // Rotate connection line.
                }
            }
        }
    }
}
