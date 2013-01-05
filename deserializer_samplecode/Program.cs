using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using dotkrizz.xml_deserializer;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using dotkrizz;
using XmlSerializer = dotkrizz.xml_deserializer.XmlSerializer;

namespace DeserializerTest {
  [Serializable]
  public class InnerTest : IXmlSourceFileLineInfo {
    [XmlAttribute]
    public int a;
    [XmlElement]
    public List<string> b;

    #region ISourceFileInfo Members

    public string BaseUri {
      get;
      set;
    }

    public int LineNumber {
      get;
      set;
    }

    public int LinePosition {
      get;
      set;
    }

    #endregion
  }

  [Serializable, XmlRoot("Test")]
  public class Test {
    [XmlAttribute]
    public string attr1;
    [XmlAttribute("attr2")]
    public int attr2;
    [XmlElement("elem1"), StoreLineInfo("elem1_info")]
    public string elem1;
    [XmlIgnore]
    public IXmlSourceFileLineInfo elem1_info;
    [XmlElement]
    public int elem2;
    [XmlElement("nested")]
    public InnerTest inner;
    [XmlArray("foos"), XmlArrayItem("foo")]
    public List<string> foos;
    [XmlArray("inners"), XmlArrayItem("inner")]
    public List<InnerTest> inners;
    [XmlAnyAttribute]
    public List<XAttribute> attrs;
    [XmlAnyElement]
    public List<XElement> elems;
  }

  public class Program {
    public static void Main(string [] args) {
      string xml = @"<Test attr1=""hello"" attr2=""25"" inny=""argument""> 
                        <elem1>wor""l""d</elem1>               
                        <elem2>27</elem2>         
                        <inny>:)</inny>
                        <jeszcze_inny>:P</jeszcze_inny>           
                        <nested a=""321"">
                            <b>!!</b>
                            <b>qwerty</b>
                        </nested>
                        <inners>
                           <inner a=""554""><b>!!</b><b>qwertz
</b></inner>
                           <inner a=""154""><b>qwa</b><b>qwe</b></inner>
                        </inners>
                        <foos><foo>x</foo><foo>y</foo><foo>z</foo></foos>   
                     </Test>";
      XmlSerializer<Test> ser = new XmlSerializer<Test>();
      StringReader sr = new StringReader(xml);
      Test t = ser.Deserialize(sr);

      // put debugger here and examine "t"

      return;
    }
  }
}
