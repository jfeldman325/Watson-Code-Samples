using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Net.Http.Formatting;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;
using System.Xml;
using System.Data;
using System.Diagnostics;
using System.Configuration;
using System.Collections.Specialized;




public class WatsonAnalyzer
{

    static public void GetAction()
    {
        try
        {
            //opens a sqlconnection at the specified location
            String SQLConnectionString = ConfigurationManager.AppSettings.Get("Key0");
            using (SqlConnection sqlConnection1 = new SqlConnection(SQLConnectionString))
            {
                SqlCommand cmd = new SqlCommand();
                SqlDataReader reader;

                //enters the GET querry from the action table and saves the response 
                cmd.CommandText = ConfigurationManager.AppSettings.Get("Key4");
                cmd.CommandType = CommandType.Text;
                cmd.Connection = sqlConnection1;
                sqlConnection1.Open();
                Console.WriteLine("connected");
                String Username = ConfigurationManager.AppSettings.Get("Key2");
                String Password = ConfigurationManager.AppSettings.Get("Key3");
                String Text= Console.ReadLine("please enter the text you would like to analyze");
                HTTP_POST(Text, Username, Password, (result) => PublishToTable(result, TicketID, UserID));
                sqlConnection1.Close();
            }
        }
        catch (Exception e1)
        {
            //string sSource = "WatsonAnalyzer";
            EventLog.WriteEntry(sSource, "Exception caught at select from ACtionsToAnalyze or HttpPOST:" + e1.Message);

        }
    }

    static void PublishToTable(Response result, String TicketID, String UserID)
        {
            //Converts the serialized json response into a list of tone objects 
            try
            {
                ToneList response = JsonConvert.DeserializeObject<ToneList>(result.WatsonResponse);
                List<Tones> ToneList = response.utterances_tone[0].tones; //creates the list of tones to be added to the database

                if (!ToneList.Any())
                {
                    Console.WriteLine("There were no sentiments for this text")
                }
                foreach (Tones item in ToneList)
                {
                    Console.WriteLine(item.tone_id);
                }
            }
            catch (Exception e)
            {
               
                EventLog.WriteEntry(sSource, "Exception at insert into Action Sentiment:" + e.Message);
 
            }

        }

    static async void HTTP_POST(String InputText, String Username, String Password, Action<Response> callback)
        {
            //Create Json Readable String with user input:    
            try
            {
                if (InputText != null || InputText != "")
                {
                    //This is the format that Watson excepts for the Json Input. The two text fields have to be formatted without any protected charecters
                    String jsonString = "{\r\n  \"utterances\": [\r\n    {\r\n      \"text\":" + "\"" + InputText + "\"" + ",\r\n      \"user\":" + "\"" + UserID + "\"" + "\r\n  }\r\n  ]\r\n}\r\n";

                    using (HttpClient client = new HttpClient())
                    {   //Establish client
                        var TargetUrl = ConfigurationManager.AppSettings.Get("Key1");

                        //Concatonate credentials and pass authorization to the client header
                        var Auth = Username + ":" + Password;
                        //Console.WriteLine("Accessing Watson with credentials:" + Auth);
                        var byteArray = Encoding.ASCII.GetBytes(Auth);
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                        //add header with input type: json
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                        //Make Post call and await response
                        using (var response = await client.PostAsJsonAsync(TargetUrl, JObject.Parse(jsonString)))
                        {
                            HttpContent content = response.Content;
                            //Format response and write to console (should be changed eventually to post to table using sql protocol
                            var formatted = response.Content.ReadAsStringAsync().Result;
                            string result = await content.ReadAsStringAsync();

                            //Create result object to organize response
                            var ResultResponse = new Response();
                            ResultResponse.ActionID = UserID;
                            ResultResponse.InputText = InputText;
                            ResultResponse.WatsonResponse = result;
                            callback(ResultResponse); //returns the response object to pass on to the postSQL class
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //string sSource = "WatsonAnalyzer";
                //EventLog.WriteEntry(sSource, "Error durring watson analysis:" + ex.Message);

            }
        }
    }
    //creates an object to format the Watson Response
public class Response
    {
        public String ActionID { get; set; }
        public String InputText { get; set; }
        public String WatsonResponse { get; set; }
    }

    //Creates the deserialize object for Json Returning from Watson
public class ToneList
    {
        public List<Utterance> utterances_tone { get; set; }
    }
public class Utterance
    {
        public List<Tones> tones { get; set; }
    }
public class Tones
    {
        public float score { get; set; }
        public String tone_id { get; set; }

    }




