using System;
using System.Data;


namespace QBort.Core.Structures
{
    public struct PlayerData
    {
        public ulong ID {get; private set;} = 0;
        public string Name {get; set;} = string.Empty;
        public int GameCount {get; private set;} = -1;
        public bool? IsBanned {get; private set;}= null;
        public string Notes {get; private set;} = string.Empty;

        public PlayerData(DataTable _player)
        {

            string valuetest
                = string.Empty;
            try
            {
                valuetest = "PlayerId";
                ID = Convert.ToUInt64(_player.Rows[0]["PlayerId"]);
                valuetest = "gamecount";
                GameCount = Convert.ToInt16(_player.Rows[0]["PlayCount"]);
                valuetest = "bancheck";
                IsBanned =
                    Convert.ToInt16(_player.Rows[0]["IsBanned"]) > 0 ? true : false;
            }
            catch(ArgumentNullException e)
            {
                Notes = string.Concat(Notes, valuetest , 
                " check returned null... Not great...\n", Messages.FormatError(e));
            }
            catch(FormatException e)
            {
                Notes = string.Concat(Notes, valuetest, 
                    " Format exception. Verify data or try 'banning and unbanning'...</s>\n", Messages.FormatError(e));
            }
            catch(OverflowException e)
            {
                string ohnoes = "... I don't know how an overflow exception was thrown... Contact an adult... Immediately..!\n";
                Notes = string.Concat(Notes, valuetest, ohnoes, Messages.FormatError(e));
            }
            finally
            {
                if (!string.IsNullOrEmpty(Notes))
                {
                    string _data = string.Concat(_player.Rows.ToString(), "\n");
                    foreach (var value in _player.Rows[0].ItemArray)
                        _data = string.Concat(_data, value.ToString(), ",\t");
                    Log.Error(string.Concat(_data, '\n', Notes));
                }
            }
        }
    }
}