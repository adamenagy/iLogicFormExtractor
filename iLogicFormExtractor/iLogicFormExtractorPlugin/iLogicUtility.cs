using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Inventor;

using Autodesk.iLogic.Core.UiBuilderStorage;
using Autodesk.iLogic.UiBuilderCore.Data;
using Autodesk.iLogic.UiBuilderCore.Storage;
using Newtonsoft.Json.Linq;

namespace iLogicFormExtractorPlugin
{
    /// <summary>
    /// Read model-specific UI data from iLogic forms that are stored in an Inventor document.
    /// </summary>
    public class iLogicFormsReader
    {
        //private readonly PictureManager _pictureManager;

        readonly UiStorage storage;
        readonly dynamic document;
        readonly string folderPath;

        /// <summary> Constructor. </summary>
        public iLogicFormsReader(Document document, string folder)
        { 
            this.storage = UiStorageFactory.GetDocumentStorage(document);
            this.document = document;
            this.folderPath = folder;
        }

       
        public string ToJsonString()
        {
            var jsonRoot = new JObject();

            // parameters
            var jsonParams = new JArray();
            var inventorParameters = document.ComponentDefinition.Parameters;
            foreach (Parameter param in inventorParameters)
            {
                var jsonParam = new JObject();
                jsonParam.Add("name", param.Name);
                jsonParam.Add("type", param.ParameterType.ToString());
                jsonParam.Add("expression", param.Expression);
                if (param.ExpressionList != null && param.ExpressionList.Count > 0)
                {
                    var jsonExpressions = new JArray();
                    foreach  (string expression in param.ExpressionList)
                    {
                        jsonExpressions.Add(expression);
                    }

                    jsonParam.Add("expressions", jsonExpressions);
                }
                jsonParams.Add(jsonParam);
            }
            jsonRoot.Add("parameters", jsonParams);

            // forms
            var jsonForms = new JArray();
            var names = storage.FormNames;
            foreach (string name in names)
            {
                var jsonForm = GetGroupsAndParameters(name);
                jsonForm.Add("name", name);
                jsonForms.Add(jsonForm);
            }
            jsonRoot.Add("forms", jsonForms);

            return jsonRoot.ToString();
        }

        /// <summary>
        /// Get groups and parameters.
        /// </summary>
        /// <remarks>
        /// This overwrites the <see cref="groups"/> and <see cref="groupsDictionary"/> class fields.
        /// </remarks>
        /// <returns>The total count of parameters on the form.</returns>
        private JObject GetGroupsAndParameters(string formName)
        {
            FormSpecification formSpec = storage.LoadFormSpecification(formName);

            return GetGroupItems(formSpec, null);
        }

        /// <summary>
        /// Get a list of all the groups that directly contain parameters.
        /// If there is a tree structure, this will produce a representation of it as a flattened list.
        /// </summary>
        private JObject GetGroupItems(UiElementContainerSpec container, UiElementContainerSpec containerToProcess = null)
        {
            JObject item = new JObject();

            if (containerToProcess == null)
                containerToProcess = container;

            var subItems = new JArray();
            foreach (var elementSpec in containerToProcess.Items)
            {
                var subItem = GetElement(container, elementSpec);
                subItems.Add(subItem);
            }

            if (subItems.Count() > 0)
            {
                item.Add("items", subItems);
            }

            return item;
        }

        private JObject GetElement(UiElementContainerSpec container, UiElementSpec elementSpec)
        {
            JObject item = null;

            var pcs = elementSpec as ParameterControlSpec;
            if (pcs != null)
            {
                item = GetParameter(container, pcs);
            }
            else if (elementSpec is PictureControlSpec)
            {
                item = GetPicture(container, (PictureControlSpec)elementSpec);
            }
            else 
            {
                var subContainer = elementSpec as UiElementContainerSpec;
                if (subContainer != null)
                {
                    var group = subContainer as ControlSpecGroupBase;
                    if (group != null)
                    {
                        item = GetGroupItems(group, null);
                    }
                    else if (subContainer is ControlRowSpec)
                    {
                        item = GetGroupItems(container, subContainer);
                    }
                    else
                    {
                        var pictureFolderSpec = subContainer as PictureFolderSpec;
                        if (pictureFolderSpec != null)
                        {
                            item = GetPictureFolder(container, pictureFolderSpec);
                        }
                    }
                }
            }

            if (item == null)
            {
                item = new JObject();
            }

            item.Add("name", elementSpec.Name);
            item.Add("tooltip", elementSpec.ToolTip);
            item.Add("displayName", elementSpec.DisplayName);
            item.Add("type", elementSpec.GetType().Name);

            return item;
        }

        private string SaveImage(Image image)
        {
            string hash;
            using (var stream = new MemoryStream())
            {
                image.Save(stream, ImageFormat.Png);

                using (MD5 md5 = MD5.Create())
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    hash = new Guid(md5.ComputeHash(stream)).ToString("N");
                }
            }

            // generate fullname for the picture
            var filename = hash + ".png";
            System.Diagnostics.Debug.WriteLine("Saving '" + filename + "'");
            var fullName = System.IO.Path.Combine(folderPath, filename);
            image.Save(fullName);

            return filename;
        }

        private JObject GetPicture(UiElementContainerSpec container, PictureControlSpec pictureSpec)
        {
            var item = new JObject();
            item.Add("pictureParameterName", pictureSpec.PictureParameterName);

            try
            {
                System.Diagnostics.Debug.WriteLine("Trying to save '" + pictureSpec.Name);
                string name = SaveImage(pictureSpec.Image.Bitmap);
                item.Add("file", name);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }

            return item;
        }

        private JObject GetPictureFolder(UiElementContainerSpec container, PictureFolderSpec pictureFolderSpec)
        {
            var item = new JObject();

            var subItems = new JArray();
            foreach (var pic in pictureFolderSpec.Items)
            {
                var pictureItem = pic as PictureSourceSpec;
                if (pictureItem == null) continue;

                try
                {
                    System.Diagnostics.Debug.WriteLine("Trying to save '" + pictureItem.Name);
                    string name = SaveImage(pictureItem.Bitmap.Bitmap);
                    var subItem = new JObject();
                    subItem.Add("name", pictureItem.Name);
                    subItem.Add("file", name);
                    subItems.Add(subItem);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                }
            }
            item.Add("items", subItems);

            return item;
        }


        private JObject GetParameter(UiElementContainerSpec container, ParameterControlSpec pcs)
        {
            var item = new JObject();
            item.Add("displayName", pcs.DisplayName);
            item.Add("type", pcs.GetType().Name);
            item.Add("alwaysReadOnly", pcs.AlwaysReadOnly);
            item.Add("readOnly", pcs.ReadOnly);
            item.Add("enablingParameterName", pcs.EnablingParameterName);
            item.Add("parameterName", pcs.ParameterName);

            NumericParameterControlSpec numeric = pcs as NumericParameterControlSpec;
            if (numeric != null)
            {
                if (numeric.EditControlType == ControlType.TrackBar)
                {
                    item.Add("minimumValue", numeric.TrackBarProperties.MinimumValue);
                    item.Add("maximumValue", numeric.TrackBarProperties.MaximumValue);
                }
                item.Add("editControlType", numeric.EditControlType.ToString());
            }

            return item;
        }
    }
}