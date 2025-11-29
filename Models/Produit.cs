using System.ComponentModel.DataAnnotations;
namespace CrudDemo.Models
{
	public class Produit
	{
		[Key]
		public int Id { get; set; }
		[Required]
		public required string Nom { get; set; }
		public decimal Prix { get; set; }
	}
}
