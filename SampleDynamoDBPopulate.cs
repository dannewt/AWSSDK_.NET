using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class SampleDynamoDBPopulate : MonoBehaviour
{
    /* Daniel Newton - 8/8/2021
     * Sample script written to integrate part metadata stored in an AWS DynamoDB Table
     * with a Unity Application so that a user could interactively update the status of
     * elements.
     * 
     * Note - Variables and elements have been changed to protect the project privacy
     * from which the sample was pulled */

    //Create a private variable for each property
    private string m_elementID;
    private string m_name;
    private string m_status;
    private string m_size;
    private string m_comments;
    private bool m_selected = false;

    //Create a new instance of the AWS Manager Class
    AWSManager _aws = new AWSManager();


    #region Properties
    //Declare the publicly accessible properties for the Class
    public string ElementID
    {
        get { return m_elementID; }
        set { m_elementID = value; }
    }
    public string Name
    {
        get { return m_name; }
        set { m_name = value; }
    }
    public string Status
    {
        get { return m_status; }
        set { m_status = value; }
    }
    public string Size
    {
        get { return m_size; }
        set { m_size = value; }
    }
    public string Comments
    {
        get { return m_comments; }
        set { m_comments = value; }
    }
    public bool Selected
    {
        get { return m_selected; }
        set { m_selected = value; }
    }

    #endregion

    #region Constructor
    /// <summary>
    /// Initialize the properties by querying the DynamoDB table
    /// and storing the table info into the public properties
    /// </summary>
    public void Init()
    {
        Dictionary<string, string> dict = new Dictionary<string, string>();

        //In this case, The element ID is stored within the object name in Unity
        m_elementID = gameObject.name.Split('[')[1].Split(']')[0];

        //The name property is the text prior to the element ID number
        m_name = gameObject.name.Split('[')[0];

        //The QueryDB method returns a dictionary for the key value pairs for
        //the specific element ID of the object
        dict = _aws.QueryDB("TrackingTable", "ElementID", ElementID);

        string key, val;
        foreach (var item in dict)
        {
            key = item.Key;
            val = item.Value;

            if (key == "Status")
                m_status = val;
            if (key == "Size")
                m_size = val;
            if (key == "Comments")
                m_comments = val;
        }
    }

    #endregion

    /// <summary>
    /// On game start, create a box collider around the object, and initialize the properties
    /// </summary>
    public void Start()
    {
        BoxCollider bc = gameObject.AddComponent<BoxCollider>() as BoxCollider;
        Init();
    }
}
