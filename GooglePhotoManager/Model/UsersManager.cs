using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GooglePhotoManager.Model
{
    /// <summary>
    /// Class related to users management.
    /// </summary>
    internal class UsersManager
    {
        #region "Private fields"

        private List<MyUser> _users = new()
        {
            new("Carlo", 704),
            new("Luca", 874),
            new("Sabrina", 433),
            new("Mauro", 432)
        };

        #endregion

        #region "Properties"

        internal List<MyUser> Users { get => _users; }

        #endregion

        #region "Constructor"

        internal UsersManager()
        {

        }

        #endregion
    }
}
