using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotcakesWinFormsApp.Helpers;

/// Egyszerű lapozási segéd: oldalszám és lapméret alapján kiszámolja a kezdő rekord indexét (offset).

public static class PaginationHelper
{
    public static int GetOffset(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 1;
        return (pageNumber - 1) * pageSize;
    }
}
