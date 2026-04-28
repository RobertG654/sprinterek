using System;
using DotNetNuke.ComponentModel.DataAnnotations;

namespace DNNHello.DNNHello.Models
{
    [TableName("DNNHello_Items")]
    [PrimaryKey("ItemId", AutoIncrement = true)]
    [Scope("ModuleId")]
    public class Item
    {
        public int ItemId { get; set; }
        public int ModuleId { get; set; }
        public string ItemName { get; set; }
        public string ItemDescription { get; set; }
        public string ImagePath { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedOnDate { get; set; }
        public int LastModifiedByUserId { get; set; }
        public DateTime LastModifiedOnDate { get; set; }
        public bool IsGlobal { get; set; }
        public bool IsUserApproved { get; set; }
        public bool IsVisibleInGlobalGallery => IsGlobal && IsUserApproved;
    }
}