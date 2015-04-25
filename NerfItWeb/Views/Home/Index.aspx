<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage<NerfItWeb.Models.PanTiltModel>"  %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
 <title>Nerf-It Viewer</title>
</head>
<body>
  <h1>Nerf-It Web</h1>

  <% using(Html.BeginForm()) { %>
  <table>
    <tr><td colspan="2">Valid Pan/Tilt ranges are 0.000 (left/top) thru 1.000 (right/bottom):</td></tr>
    <tr><th>Pan</th><td><%= Html.TextBox("PanTextbox", String.Format("{0:N3}", Model.Pan)) %></td></tr>
    <tr><th>Tilt</th><td><%= Html.TextBox("TiltTextbox", String.Format("{0:N3}", Model.Tilt)) %></td></tr>
    <tr><td align="right"><input type="submit" name="submit" value="Fire" /></td><td><input type="submit" name="submit" value="GoTo"/></td></tr>
  </table>
  <% } %>
  <img src="/Home/Image/image.jpg" id="WebcamImage" />
    <script type="text/javascript">
    var newImage = new Image();
    newImage.src = "/Home/Image/image.jpg";

    function ImageRefresh()
    {
      HistoryRefresh();
      if(newImage.complete){
        ImageElement.src=newImage.src;
        newImage = new Image();
        newImage.src = "/Home/Image/image.jpg?time=" + new Date().getTime();
      }
      setTimeout(ImageRefresh,2000);
    }

    function HistoryRefresh() {
      var oXMLHttpRequest = new XMLHttpRequest;
      oXMLHttpRequest.open("GET", "/Home/History?time=" + new Date().getTime(), false);
      oXMLHttpRequest.onreadystatechange  = function() {
       if (this.readyState == XMLHttpRequest.DONE) {
         document.getElementById('HistoryDiv').innerHTML = this.responseText;
       }
      }
      oXMLHttpRequest.send(null);
    }

    var ImageElement = document.getElementById('WebcamImage');
    ImageRefresh();
  </script>
  <br/>
  <div id="HistoryDiv">
  <%
    foreach(var history in Model.History)
    {
      Response.Write(history + "<br/>");
    }
  %>
  </div>
</body>

