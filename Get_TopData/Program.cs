using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.Threading;

namespace Get_TopData
{
    internal class Program
    {
        static void Main(string[] args)
        {
            do
            {
                Task t = taskUpdate();
                t.Start();
                t.Wait();
                GC.Collect();
                Thread.Sleep(TimeSpan.FromHours(1));
                
            }while (true);

        }
        public static Task taskUpdate()
        {
            Task t = new Task(() =>
            {
                File.Copy(Directory.GetCurrentDirectory() + "\\G_Data.mdf", Directory.GetCurrentDirectory() + "\\BookingTop.mdf",true);
                File.Copy(Directory.GetCurrentDirectory() + "\\G_Data_log.ldf", Directory.GetCurrentDirectory() + "\\BookingTop_log.ldf", true);
                var sqlconnectstring = $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={Directory.GetCurrentDirectory() + "\\BookingTop.mdf"};Integrated Security=True;Connect Timeout=30";
                HttpClientHandler handler = new HttpClientHandler();
                CookieContainer ck = new CookieContainer();
                handler.CookieContainer = ck;
                HttpClient client = new HttpClient(handler);
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/102.0.0.0 Safari/537.36");
                HttpRequestMessage mgs = new HttpRequestMessage(HttpMethod.Post, new Uri("https://v15.topocean.com/WebBooking/SysLogin.asp"));
                var parameters = new List<KeyValuePair<string, string>>()
                {
                new KeyValuePair<string, string>("txtLoginName", "spanx_deltavnm"),
                new KeyValuePair<string, string>("txtPassword", "QST+-3+tD9"),
                new KeyValuePair<string, string>("hdnProcess", "Process"),
                new KeyValuePair<string, string>("redirect", "")
                };
                mgs.Content = new FormUrlEncodedContent(parameters);
                var req = client.SendAsync(mgs).Result;
                req.EnsureSuccessStatusCode();
                Console.WriteLine(req.StatusCode);
                mgs = new HttpRequestMessage(HttpMethod.Get, new Uri("https://v15.topocean.com/WebBooking/BookingList.asp"));
                req = client.SendAsync(mgs).Result;
                List<string> table = new List<string>();
                SortedList<string, List<KeyValuePair<string, string>>> sort = new SortedList<string, List<KeyValuePair<string, string>>>();
                string[] data = req.Content.ReadAsStringAsync().Result.Split('<');
                for (int vak = 0; vak < data.GetLength(0); vak++)
                {
                    if (data[vak].Contains("main_bookingListTable"))
                    {
                        for (int jav = vak; jav < data.GetLength(0); jav++)
                        {
                            if (!data[jav].Contains("ListData"))
                            {
                                if (data[jav].Contains("BookingReqID") && !data[jav].Contains("MakeBooking"))
                                {
                                    table.Add(data[jav].Split('>')[1]);
                                    Console.WriteLine("----------------------------------------------------------------------------------");
                                    List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
                                    mgs = new HttpRequestMessage(HttpMethod.Get, new Uri($"https://v15.topocean.com/WebBooking/BookingDetail.asp?BookingReqID={data[jav].Split('>')[1]}"));
                                    req = client.SendAsync(mgs).Result;
                                    string[] tabledetail = req.Content.ReadAsStringAsync().Result.Split('<');
                                    for (int vak1 = 0; vak1 < tabledetail.GetLength(0) - 2; vak1++)
                                    {
                                        if (tabledetail[vak1].Contains("fieldLabel"))
                                        {
                                            KeyValuePair<string, string> key_value = new KeyValuePair<string, string>();
                                            string title_value = "";
                                            if (tabledetail[vak1 + 2].Split('>')[1].Length > 1)
                                            {
                                                title_value = string.Join("", string.Join("", (tabledetail[vak1].Split('>')[1] + "_" + tabledetail[vak1 + 2].Split('>')[1]).Split('\n')).Split('\r'));
                                            }
                                            else
                                            {
                                                title_value = string.Join("", string.Join("", (tabledetail[vak1].Split('>')[1] + "_#").Split('\n')).Split('\r'));
                                            }
                                            Console.WriteLine(title_value);
                                            key_value = new KeyValuePair<string, string>(title_value.Split('_')[0], title_value.Split('_')[1]);
                                            list.Add(key_value);
                                        }
                                        if (tabledetail[vak1].Contains("Equipment Type & Size & Quantity"))
                                        {
                                            KeyValuePair<string, string> key_value = new KeyValuePair<string, string>();
                                            string title_value1 = "";
                                            string title_value2 = "";
                                            int contv = 0;
                                            for (int vkl = vak1; vkl < tabledetail.GetLength(0) - 2; vkl++)
                                            {
                                                contv++;
                                                if (tabledetail[vkl].Length > 3)
                                                {
                                                    if (tabledetail[vkl].Substring(0, 1) == "b")
                                                    {
                                                        title_value1 = title_value1 + tabledetail[vkl].Substring(2, tabledetail[vkl].Length - 2) + ";";
                                                   
                                                    }
                                                    if (tabledetail[vkl].Substring(0, 3) == "/tr")
                                                    {
                                                        break;
                                                    }
                                                }
                                            }
                                            int count2 = title_value1.Split(';').GetLength(0)-2;
                                            for (int vkl = vak1+ contv; vkl < tabledetail.GetLength(0) - 2; vkl++)
                                            {
                                                if (tabledetail[vkl].Length > 3)
                                                {
                                                    
                                                    if (tabledetail[vkl].Substring(0, 2) == "td" && tabledetail[vkl].Contains("text-align"))
                                                    {
                                                        title_value2 = title_value2 + tabledetail[vkl].Split('\"')[2].Split('>')[1] + ";";
                                                        count2--;
                                                        if (count2 == 0)
                                                        {
                                                            title_value2 = title_value2 + "\n";
                                                            count2 = title_value1.Split(';').GetLength(0)-2;
                                                        }
                                                    }
                                                    if (tabledetail[vkl].Substring(0, 4) == "/tab")
                                                    {
                                                        break;
                                                    }
                                                }
                                               
                                            }
                                            if(title_value1.Split(';').GetLength(0)== title_value2.Split(';').GetLength(0)+1)
                                            {
                                                title_value2 = title_value2 + ";";
                                                for (int time = 0; time < title_value1.Split(';').GetLength(0); time++)
                                                {
                                                    key_value = new KeyValuePair<string, string>(title_value1.Split(';')[time], title_value2.Split(';')[time]);
                                                    list.Add(key_value);
                                                }
                                            }
                                            else
                                            {
                                                for (int time = 0; time < title_value1.Split(';').GetLength(0)-1; time++)
                                                {
                                                    string title_value3 = "";
                                                    for (int timex = 0; timex < title_value2.Split('\n').GetLength(0); timex++)
                                                    {
                                                        string line = title_value2.Split('\n')[timex];
                                                        if (line != "")
                                                        {
                                                            title_value3 = title_value3 + line.Split(';')[time] + "/";
                                                        }
                                                    
                                                    }
                                                    key_value = new KeyValuePair<string, string>(title_value1.Split(';')[time], title_value3);
                                                    list.Add(key_value);
                                                }
                                                
                                            }
                                            
                                        }
                                    }
                                    sort.Add(data[jav].Split('>')[1], list);
                                }

                            }
                            else { break; }
                        }
                        break;
                    }
                }
                
                string sea = "BookingID#:,BookingDate:,WeekofYear:,BookingConfirmation:,BookingStatus:,MBL:,TransportationMode:,Non-Managed:,HBL:,OriginOffice:,DestOffice:,Sub-DestOffice:,ReleaseType:,BookingParty:,Term:,Shipper:,VendorCode:,Consignee:,PlaceofReceipt:,PlaceofDelivery:,DeliveryType:,Commodity:,EquipmentType/Qty:,ShippedType/Qty:,Commodity2:,EquipmentType/Qty:,ShippedType/Qty:,VendorReference:,POReadyDate:,LoadPort:,CurrentETD:,ATD:,DischPort:,CurrentETA:,ATA:,Carrier:,Vessel:,Voyage:,CustomerRemarks:,FeederVessel:,ApprovalStatus:,ApprovalDate:,ApprovedBy:,ApprovalRemark:\n";
                string air = "BookingID#:,Booking Date:,Week of Year:,Booking Confirmation:,Booking Status:,MAWB:,Transportation Mode :,Non-Managed:,AWB:,Origin Office:,Dest Office:,Sub-Dest Office:,Release Type:,Booking Party:,Term:,Shipper:,Vendor Code:,Consignee :,Place of Receipt:,Place of Delivery:,Delivery Type:,Commodity:,Vendor Reference :,PO Ready Date:,No. Of Pieces:,Gross Wgt(kg):,Volume(CBM):,Departure Port:,Current ETD:,ATD:,Arrival Port,:,Current ETA:,ATA:,Airline:,Flight No:,Customer Remarks :,Approval Status :,Approval Date:,Approved By:,Approval Remark :\n";
                string jk = "";
                foreach (var klo in sort)
                {
                    WriteDatabase(sqlconnectstring, klo.Value, klo.Key);
                    foreach (var item in klo.Value)
                    {
                        DateTime time = new DateTime();
                        if(DateTime.TryParse(item.Value,out time))
                        {
                            string datetimevalue = time.ToString().Split(' ')[0];
                            jk = jk + datetimevalue + ",";
                        }
                        else
                        {
                            jk = jk + string.Join("", string.Join(" ", string.Join("", item.Value.Split(',')).Split('\t')).Split('\n')).Trim()+",";
                        }
                    }
                    jk = jk + "\n";
                    if (jk.Contains("SEA"))
                    {
                        sea = sea + jk;
                    }
                    else
                    {
                        air = air + jk;
                    }
                    jk = "";
                }
                File.WriteAllText(Directory.GetCurrentDirectory() + $"\\{DateTime.Now.Month.ToString()}_{DateTime.Now.Day.ToString()}_{DateTime.Now.Hour.ToString()}_ Repost Booking.csv", sea+"\n"+air);
            });
            return t;
            
        }
        public static void WriteDatabase(string conv, List<KeyValuePair<string, string>> list , string Idbk)
        {
            
            string querry = @"INSERT INTO [BookingTop] ([Booking_Id],[Mode],[MBL],[HBL],[ETD],[ETA],[Booking_Confirmation]) VALUES (@BookingId,@Mode,@MBL,@HBL,@CurrentETD,@CurrentETA,@BookingConfirmation)";
            using (var connection = new SqlConnection(conv))
            {
                connection.Open();
                connection.StatisticsEnabled = true;
                using (var comment = new SqlCommand())
                {
                    comment.CommandText = querry;
                    comment.Connection = connection;
                    comment.Parameters.AddWithValue("@BookingId", Idbk);
                    foreach (var mk in list)
                    {
                        if (mk.Key.Contains("Mode"))
                        {
                            comment.Parameters.AddWithValue("@Mode", mk.Value);
                        }
                        if (mk.Key.Contains("ETD"))
                        {
                            comment.Parameters.AddWithValue("@CurrentETD", mk.Value);
                        }
                        if (mk.Key.Contains("ETA"))
                        {
                            comment.Parameters.AddWithValue("@CurrentETA", mk.Value);
                        }
                        if (mk.Key.Contains("Confirmation"))
                        {
                            comment.Parameters.AddWithValue("@BookingConfirmation", mk.Value);
                        }
                        if (mk.Key.Contains("MAW"))
                        {
                            comment.Parameters.AddWithValue("@MBL", mk.Value);
                        }
                        if (mk.Key.Contains("AWB")&&!mk.Key.Contains("MAW"))
                        {
                            comment.Parameters.AddWithValue("@HBL", mk.Value);
                        }
                        if (mk.Key.Contains("MBL"))
                        {
                            comment.Parameters.AddWithValue("@MBL", mk.Value);
                        }
                        if (mk.Key.Contains("HBL"))
                        {
                            comment.Parameters.AddWithValue("@HBL", mk.Value);
                        }
                    }
                    try
                    {
                        comment.ExecuteNonQuery();
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(Idbk);
                    }
                }
                connection.Close();
            }
        }
    }
}
