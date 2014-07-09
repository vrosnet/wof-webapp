﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.IO;
using System.Xml.Linq;
using System.Xml;

namespace PathFinding
{
    /*
     * For now, this class supports only svg files created by Microsoft Visio, in that specific format.
     * Later it is planned to develop a tool for easy creation of simple SVG-maps in a set format.
     */
    public class Converter
    {
        private double epsilon;

        private Graph graph;
        public Graph Graph
        {
            get { return graph; }
        }

        private List<Line> lines;
        private double scale;

        public double Scale
        {
            get { return scale; }
            set { scale = value;  }
        }

        public Converter()
        {
            graph = new Graph();
            lines = new List<Line>();
        }
        public static Graph downloadMap(string filePath, double scale, double epsilon)
        {
            
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.DtdProcessing = DtdProcessing.Ignore;
            XmlReader reader = XmlReader.Create(filePath, settings);
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);
            Converter converter = new Converter();
            converter.Scale = scale;
            converter.epsilon = epsilon;
            converter.transferData(doc);
            converter.generateEdges();
            return converter.graph;
        }

        public void transferData(XmlDocument doc)
        {

            XmlNode main = doc.LastChild;
            XmlNodeList main_nodes = main.ChildNodes;
            XmlElement g_tag = (XmlElement)main_nodes[5];
            XmlNodeList g_tags = g_tag.GetElementsByTagName("g");
            
            processGTags(g_tags);

        }

        private void processGTags(XmlNodeList g_tags)
        {

            for (int i = 0; i < g_tags.Count; i++)
            {
                XmlElement g_tag =(XmlElement) g_tags[i];

                string transform = "";
                if (g_tag.HasAttribute("transform"))
                {
                    transform = g_tag.GetAttribute("transform");
                }

                //Point translate = extractTranslate(transform);
                //double rotationAngle = extractRotate(transform);

                int officeNumber = -1;
                XmlNodeList v_tags = g_tag.GetElementsByTagName("v:cp");
                if( v_tags.Count != 0)
                {
                    XmlElement v_tag = (XmlElement)v_tags[0];
                    officeNumber = extractOfficeNumber(v_tag.GetAttribute("v:val"));
                }

                XmlNodeList path_tags = g_tag.GetElementsByTagName("path");
                XmlElement path = (XmlElement)path_tags[0];                                  
                string path_data = path.GetAttribute("d");
                processPath(path_data, transform, officeNumber);
               
            }
            int a = 1;
        }



        private void processPath(string path, string transformationData, int officeNumber)
        {
            string[] coordinates = path.Split(' ');

            if (coordinates.Length <= 1)
            {
                throw new Exception();
            }

            Point current_pt = new Point(getCoordinateFromString(coordinates[0]), getCoordinateFromString(coordinates[1]));
            transform(transformationData, current_pt);


            for (int i = 2; i < coordinates.Length; i += 2)
            {
                Point end_pt = new Point(getCoordinateFromString(coordinates[i]), getCoordinateFromString(coordinates[i + 1]));
                transform(transformationData, end_pt);
                // end_pt.translate(translationOffset.getX(), translationOffset.getY());
               // end_pt.rotate(rotationAngle);

                if (!startNewLine(coordinates[i]))
                { 

                    Line curr_line = new Line(current_pt, end_pt, officeNumber);
                    

                    for (int j = 0; j < lines.Count; j++ )
                    {
                        Point crossing_pt = curr_line.crosses(lines[j], epsilon);
                        if (!string.IsNullOrEmpty(Convert.ToString(crossing_pt)))
                        {
                            Node new_node = new Node();
                            new_node.CrossingPoint = crossing_pt;
                            if (curr_line.getOfficeNumber() != -1) 
                            {
                                new_node.OfficeLocation = curr_line.getOfficeNumber();
                            }
                            else if (lines[j].getOfficeNumber() != -1)
                            {
                                new_node.OfficeLocation = lines[j].getOfficeNumber();
                            }
                            graph.addNode(new_node);
                        }
                    }
                    lines.Add(curr_line);
                }
                
                current_pt = end_pt;
            }
        }

        public static int extractOfficeNumber(string officeNumberData)
        {
            int result = -1;
            string[] officeNumberData_words = officeNumberData.Split('(', ')');
            if (officeNumberData_words.Length >= 2)
            {
                result = Convert.ToInt32(officeNumberData_words[1]);
            }

            return result;
            
        }


        public static Point extractTranslate(string transformData)
        {
            Point result = new Point(0, 0);

            string[] words = transformData.Split(' ', ')', '(', ',');

            if (words.Count() >= 3 && words[0] == "translate")
            {
                result.X = (float)Convert.ToDouble(words[1]);
                result.Y = (float)Convert.ToDouble(words[2]);
            }

            return result;
        }


        public static double extractRotate(string transformData)
        {
            double result = 0;

            string[] words = transformData.Split(' ', ')', '(', ',');

            if (words.Count() >= 7 && words[4] == "rotate")
            {
                result = Convert.ToDouble(words[5]);
            }

            return result;
        }

        public static void transform(string transformData, Point point)
        {
            float transform_x = 0;
            float transform_y = 0;
            float rotation = 0;

            string[] words = transformData.Split(' ', ')', '(', ',');

            if (words.Count() >= 3 && words[0] == "translate")
            {
                transform_x = ((float)Convert.ToDouble(words[1]));
                transform_y = ((float)Convert.ToDouble(words[2]));
            }
            if (words.Count() >= 7 && words[4] == "rotate")
            {
                rotation = (float)Convert.ToDouble(words[5]);    
            }

            point.transform(transform_x, transform_y, rotation);
        }

        public static bool startNewLine(string coordinate_text)
        {
            if (coordinate_text.Length == 0)
            {
                return false;
            }
            return (coordinate_text.Substring(0, 1) == "M");
        }

        public static float getCoordinateFromString(string coordinate_text)
        {
            if (coordinate_text.Length == 0)
            {
                throw new Exception();
            }

            if (Char.IsLetter(coordinate_text, 0))
            {
                coordinate_text = coordinate_text.Substring(1, coordinate_text.Length - 1);
                if (coordinate_text.Length == 0)
                {
                    throw new Exception();
                }
            }

            return (float)Convert.ToDouble(coordinate_text);

        }

        public void generateEdges()
        {
            for (int i = 0; i < lines.Count; i++)
            {

                SortedNodeContainer container = new SortedNodeContainer(epsilon);
                for (int j = 0; j < graph.Nodes.Count; j++)
                {
                    if (lines[i].contains(graph.Nodes[j].CrossingPoint, epsilon))
                    {
                        container.Add(graph.Nodes[j]);
                    }
                }
                if (container.Count > 1)
                {
                    for (int j = 0; j < container.Count - 1; j++)
                    {
                        graph.addEdge(container[j], container[j + 1], scale);
                    }
                }
            }
        }

        public class SortedNodeContainer : List<Node>
        {
            private double epsilon;

            public SortedNodeContainer(double epsilon) 
            {
                this.epsilon = epsilon;
            }

            public new void Add(Node newNode)
            {
                if (Count <= 1 )
                {
                    base.Add(newNode);
                }
                else
                {
                    int first_ind = 0;
                    int last_ind = Count - 1;
                    if (!(new Line(this[first_ind].CrossingPoint, this[last_ind].CrossingPoint).contains(newNode.CrossingPoint, epsilon)))
                    {
                        if (new Line(newNode.CrossingPoint, this[last_ind].CrossingPoint).contains(this[first_ind].CrossingPoint, epsilon))
                            Insert(0, newNode);
                        else
                            base.Add(newNode);
                    }
                    else
                    {
                        while (last_ind - first_ind > 1)
                        {
                            int mid_ind = first_ind + ((last_ind - first_ind) / 2);
                            if (new Line(this[first_ind].CrossingPoint, this[mid_ind].CrossingPoint).contains(newNode.CrossingPoint, epsilon))
                            {
                                last_ind = mid_ind;
                            }
                            else
                            {
                                first_ind = mid_ind;
                            }

                        }
                        Insert(last_ind, newNode);
                    }
                    
                }
               
            }
        }

    }


}