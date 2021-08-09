using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Amazon.Runtime;

public class AWSManager : MonoBehaviour
{
    public string IdentityPoolId = "";
    private string accessKeyId = "";
    //Do NOT Delete, do NOT Share
    private string secretAccessKey = "";
    public static string bucketName = "";
    //public static string keyName = "";
    public static string keyName2 = "";
    public string AWSRegion = RegionEndpoint.USEast2.SystemName;


    #region Constructors & Set-up
    private static AWSManager _instance;
    public static AWSManager Instance
    {
        get
        {
            if (_instance == null)
                Debug.Log("null");
            return _instance;
        }
    }

    private RegionEndpoint _S3Region
    {
        get
        {
            return RegionEndpoint.GetBySystemName(AWSRegion);
        }
    }

    private AmazonS3Client _S3Client;

    public AmazonS3Client S3Client
    {
        get
        {
            if (_S3Client == null)
                _S3Client = new AmazonS3Client(accessKeyId, secretAccessKey, RegionEndpoint.USEast2);
            return _S3Client;
        }
    }

    private AmazonDynamoDBClient _DynamoDBClient;

    public AmazonDynamoDBClient DynamoDBClient
    {
        get
        {
            if (_DynamoDBClient == null)
                _DynamoDBClient = new AmazonDynamoDBClient(accessKeyId, secretAccessKey, RegionEndpoint.USEast2);
            return _DynamoDBClient;
        }
    }

    private AmazonSimpleEmailServiceV2Client _SimpleEmailClient;

    public AmazonSimpleEmailServiceV2Client SimpleEmailClient
    {
        get
        {
            if (_SimpleEmailClient == null)
                _SimpleEmailClient = new AmazonSimpleEmailServiceV2Client(accessKeyId, secretAccessKey, RegionEndpoint.USEast2);
            return _SimpleEmailClient;
        }
    }

    private AmazonLambdaClient _LambdaClient;

    public AmazonLambdaClient LambdaClient
    {
        get
        {
            if (_LambdaClient == null)
                _LambdaClient = new AmazonLambdaClient(accessKeyId, secretAccessKey, RegionEndpoint.USEast2);
            return _LambdaClient;
        }
    }
    #endregion

    #region S3 Methods
    /// <summary>
    /// Download object from S3 Bucket
    /// </summary>
    /// <param name="bucketName">Name of the S3 Bucket to be accessed</param>
    /// <param name="downloadKey">Name of the item key to be downloaded</param>
    /// <returns>GetObjectResponse variable of the item key requested</returns>
    public GetObjectResponse Download(string bucketName, string downloadKey)
    {
        //Create Get Request
        GetObjectRequest request = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = downloadKey
        };

        //Asynchronously make the request to the S3 Client
        var task = S3Client.GetObjectAsync(request);
        task.Wait();

        //Return the file as a GetObjectResponse variable
        var response = task.Result;
        return response;
    }

    /// <summary>
    /// Upload/Overwrite new file to S3 Bucket
    /// </summary>
    /// <param name="path">Path to file intended to be uploaded</param>
    /// <param name="bucketName">Name of the S3 Bucket to be accessed</param>
    /// <param name="keyName">Key name for the S3 Upload (overwrite or new file)</param>
    public void UploadS3(string path, string bucketName, string keyName)
    {
        //Create Put Request
        PutObjectRequest request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = keyName,
            FilePath = path
        };

        //Asynchronously make the request to S3 Client
        var task = S3Client.PutObjectAsync(request);
        task.Wait();

        //Return the response and verify HTTP status code in Debug
        var response = task.Result;
        Debug.Log(response.HttpStatusCode);
    }

    /// <summary>
    /// Delete an existing file from the S3 Bucket
    /// </summary>
    /// <param name="bucketName">Name of the S3 Bucket to be accessed</param>
    /// <param name="keyName">Key name for the S3 Upload (overwrite or new file)</param>
    public void DeleteS3(string bucketName, string keyName)
    {
        //Create Delete Request
        DeleteObjectRequest request = new DeleteObjectRequest
        {
            BucketName = bucketName,
            Key = keyName
        };

        //Asynchronously make the request to S3 Client
        var task = S3Client.DeleteObjectAsync(request);
        task.Wait();

        //Return the response and verify status in Debug
        var response = task.Result;
        Debug.Log("Object Deleted: " + response.DeleteMarker);
    }

    #endregion

    #region DynamoDB Methods

    /// <summary>
    /// Write any attribute changes back to a DynamoDB Table
    /// </summary>
    /// <param name="tableName">Name of the DynamoDB Table in AWS</param>
    /// <param name="key">Name of the existing key</param>
    /// <param name="updates">AttribuleValueUpdates to be written</param>
    public void UpdateDB(string tableName, Dictionary<string, AttributeValue> key, Dictionary<string, AttributeValueUpdate> updates)
    {
        //Create the Update Request
        UpdateItemRequest request = new UpdateItemRequest
        {
            TableName = tableName,
            Key = key,
            AttributeUpdates = updates,
        };

        //Asynchronously send the request via DynamoDB Client
        var task = DynamoDBClient.UpdateItemAsync(request);
        task.Wait();

        //Return the result
        var response = task.Result;
        Debug.Log("AWS Server Updated");
    }

    /// <summary>
    /// Query a Database for an equivalent hash key condition
    /// </summary>
    /// <param name="tableName">Name of the DynamoDB Table in AWS</param>
    /// <param name="key">Name of the hash key</param>
    /// <param name="value">Value corresponding to the hash key</param>
    /// <returns><string,string> Dictionary of the key/value pairs queried</returns>
    public Dictionary<string,string> QueryDB(string tableName, string key, string value)
    {
        AttributeValue hashKey = new AttributeValue { S = value };

        Dictionary<string, Condition> keyConditions = new Dictionary<string, Condition>
        {
            // Hash key condition. ComparisonOperator must be "EQ".
            {
                key,
                new Condition
                {
                    ComparisonOperator = "EQ",
                    AttributeValueList = new List<AttributeValue> { hashKey }
                }
            },
        };

        QueryRequest request = new QueryRequest
        {
            TableName = tableName,
            KeyConditions = keyConditions,
        };

        var task = DynamoDBClient.QueryAsync(request);
        task.Wait();
        var response = task.Result;
        
        //NOTE: response returns a dictionary of <string, AttributeValue>
        //The following code is only necessary to return a dictionary of
        //<string, string>
        List<Dictionary<string, AttributeValue>> items = response.Items;
        Dictionary<string, string> dict = new Dictionary<string, string>();
        List<string> keyList = new List<string>();
        List<string> valList = new List<string>();
        foreach (Dictionary<string, AttributeValue> keyValuePair in items)
        {
            foreach (string tableKey in keyValuePair.Keys)
            {
                keyList.Add(tableKey);
            }

            foreach (AttributeValue val in keyValuePair.Values)
            {
                valList.Add(val.S);
            }

            for (int i = 0; i < keyList.Count; i++)
            {
                dict.Add(keyList[i], valList[i]);
            }
        }

        return dict;
    }
    #endregion

    #region SimpleEmail Methods

    /// <summary>
    /// Send a simple notification email to a specified address
    /// </summary>
    /// <param name="destinationEmailAddress"></param>
    /// <param name="senderEmailAddress"></param>
    /// <param name="subject"></param>
    /// <param name="body"></param>
    public void SendNotification(string destinationEmailAddress, string senderEmailAddress, string subject, string body)
    {

        var sendRequest = new SendEmailRequest
        {
            FromEmailAddress = senderEmailAddress,
            Destination = new Destination
            {
                ToAddresses = new List<string> { destinationEmailAddress },
            },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = subject },
                    Body = new Body
                    {
                        Html = GenerateEmailHTML(senderEmailAddress, body),
                        Text = GenerateEmailText(senderEmailAddress, body)
                    }
                }
            }
        };

        var task = SimpleEmailClient.SendEmailAsync(sendRequest);
        task.Wait();
        var response = task.Result;
        Debug.Log("Email Notification Sent");
    }

    /// <summary>
    /// Creates an AWS Content variable into a specified format using string inputs for
    /// whom the email is from, and the content to be delivered
    /// </summary>
    /// <param name="sender">email or username of the sender</param>
    /// <param name="content">content that is to be delivered</param>
    /// <returns>HTML stylized content from user input</returns>
    public Content GenerateEmailHTML(string sender, string content)
    {
        string bodyHTML = null;
        bodyHTML =
            "<html>" +
            "<body>" +
            "<h1>Limbach Productivity Tracker Bug Report</h1>" +
            "<p>The following Bug Report was sent by user: " + sender + "</p>" +
            "<p></br>" + content + "</p>" +
            "</body>" +
            "</html>";

        Content emailContent = new Content { Charset = "UTF-8", Data = bodyHTML };

        return emailContent;
    }

    /// <summary>
    /// Creates an AWS Content variable into a specified format using string inputs for
    /// whom the email is from, and the content to be delivered
    /// </summary>
    /// <param name="sender">email or username of the sender</param>
    /// <param name="content">content that is to be delivered</param>
    /// <returns>Generic Text content from user input</returns>
    public Content GenerateEmailText(string sender, string content)
    {
        string body = null;
        body =
            "Limbach Productivity Tracker Bug Report\r\n" +
            "The following Bug Report was sent by user: " + sender +
            "\n\n" + content;

        Content emailContent = new Content { Charset = "UTF-8", Data = body };

        return emailContent;
    }
    #endregion
}
