﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SeafClient.Types;

namespace SeafClient
{
    /// <summary>
    ///     Base class for Seafile web api requests
    /// </summary>
    public abstract class SeafRequest<TResponse> : ISeafRequest
    {
        public abstract string CommandUri { get; }

        /// <summary>
        ///     The http method to execute this command with
        ///     (default is GET)
        /// </summary>
        public virtual HttpAccessMethod HttpAccessMethod => HttpAccessMethod.Get;        

        /// <summary>
        ///     Get additional header values to send with this command
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<KeyValuePair<string, string>> GetAdditionalHeaders()
        {
            // we expect a response in JSON format
            yield return new KeyValuePair<string, string>("Accept", "application/json; indent=4");
        }

        /// <summary>
        ///     Return the parameters which should be encoded in the message nody                
        ///     (usually, only used if HttpAccessMethod is Post or Put)
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<KeyValuePair<string, string>> GetBodyParameters()
        {
            yield break;
        }

        /// <summary>
        ///     Used to send the request when HttpAccessMethod is Custom
        /// </summary>
        /// <returns></returns>
        public virtual HttpRequestMessage GetCustomizedRequest(Uri serverUri)
        {
            throw new NotImplementedException("SendRequestCustomized has not been implemented for this request.");
        }

        /// <summary>
        ///     Returns if the command which was answered with the given response message was successful
        /// </summary>
        public virtual bool WasSuccessful(HttpResponseMessage msg)
        {
            return msg.IsSuccessStatusCode;
        }

        /// <summary>
        ///     Returns the error description for the given response message if the command
        ///     was not executed successfully
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public virtual SeafError GetSeafError(HttpResponseMessage msg)
        {
            switch (msg.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    return new SeafError(msg.StatusCode, SeafErrorCode.ProtocolError);
                default:
                    return new SeafError(msg.StatusCode, SeafErrorCode.NoDetails);
            }
        }

        /// <summary>
        /// Indicates whether this request is supported by a seafile server with the given version or not
        /// </summary>        
        public virtual bool SupportedWithServerVersion(Version version)
        {
            return true;
        }

        /// <summary>
        ///     Read the server response for this command
        /// </summary>
        public virtual async Task<TResponse> ParseResponseAsync(HttpResponseMessage msg)
        {
            // try to read the response content as JSON object
            var stream = await msg.Content.ReadAsStreamAsync();
            using (StreamReader sr = new StreamReader(stream))
            { 

#if DEBUG
                // print response text to the console in debug mode
                var responseStr = sr.ReadToEnd();
                Debug.WriteLine("RESPONSE to " + this.GetType().Name);
                Debug.WriteLine(responseStr);
                Debug.WriteLine("========");
                sr.BaseStream.Position = 0;
#endif

                using (JsonReader reader = new JsonTextReader(sr))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    return serializer.Deserialize<TResponse>(reader);
                }
            }           
        }
    }

    public enum HttpAccessMethod
    {
        /// <summary>
        ///     The request is with a custom HttpRequestMessage
        /// </summary>
        Custom,

        /// <summary>
        ///     The request has to be sent with an HTTP GET request
        /// </summary>
        Get,

        /// <summary>
        ///     The request has to be sent with an HTTP POST request
        /// </summary>
        Post,

        /// <summary>
        ///     The request has to be sent with an HTTP PUT request
        /// </summary>
        Put,

        /// <summary>
        ///     The request has to be sent with an HTTP DELETE request
        /// </summary>
        Delete
    }
}