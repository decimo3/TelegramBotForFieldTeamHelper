namespace telbot.Helpers
{
  public static class TableMaker<T>
  {
    public static String Serialize(List<T> lista, char separador)
    {
      if (lista == null || lista.Count == 0)
        throw new ArgumentException("The list of reports cannot be null or empty.");
      var table = new System.Text.StringBuilder();
      var nomes_atributos = Cabecalho(lista.First());
      table.Append(String.Join(separador, nomes_atributos.Keys.ToList()));
      table.Append('\n');
      foreach(var item in lista)
      {
        table.Append(String.Join(separador, Rodape(item, nomes_atributos)));
        table.Append('\n');
      }
      return table.ToString();
    }
    private static Dictionary<String, Int32> Cabecalho(T obj)
    {
      if (obj == null)
        throw new ArgumentException("The report cannot be null.");
      var contador = 0;
      Type tipo = obj.GetType();
      var nomes_atributos = new Dictionary<String, Int32>();
      var atributos = tipo.GetProperties();
      foreach(var atributo in atributos)
      {
        nomes_atributos.Add(atributo.Name, contador);
        contador++;
      }
      return nomes_atributos;
    }
    private static List<String> Rodape(T obj, Dictionary<String,Int32> keys)
    {
      if (obj == null)
        throw new ArgumentException("The report cannot be null.");
      Type tipo = obj.GetType();
      var rodape = new List<String>(new string[keys.Count]);
      var atributos = tipo.GetProperties();
      foreach(var atributo in atributos)
      {
        var index = keys[atributo.Name];
        var value = atributo.GetValue(obj);
        if(value != null)
        {
          rodape[index] = value.ToString() ?? String.Empty;
        }
      }
      return rodape;
    }
  }
}