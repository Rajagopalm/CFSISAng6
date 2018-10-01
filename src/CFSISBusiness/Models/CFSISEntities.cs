using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CFSISBusiness
{

public enum Gender
{
    Male, Female
}

public enum PaymentMode
{
    Cash, Cheque, DemandDraft
}

public class Album
{           
    public int Id { get; set; }
    public int ArtistId { get; set; }        
    public string Title { get; set; }
    public string Description { get; set; }
    public int Year { get; set; }
    public string ImageUrl { get; set; }
    public string AmazonUrl { get; set; }
    public string SpotifyUrl { get; set; }

    public virtual Artist Artist { get; set; }
    public virtual IList<Track> Tracks { get; set; }

    public Album()
    {
        Artist = new Artist();
        Tracks = new List<Track>();
    }

}
    
public class Artist
{
    public int Id { get; set; }
    public string ArtistName { get; set; }
    public string Description { get; set; }
    public string ImageUrl { get; set; }
    public string AmazonUrl { get; set; }

    //public List<Album> Albums { get; set; }
}

public class ArtistWithAlbumCount : Artist
{
    public int AlbumCount { get; set; }
}

public class DistrictWithStudentCount : District
{
    public int StudentCount { get; set; }
}

public class Track
{
    public int Id { get; set; }
    public int AlbumId { get; set; }                
    public string SongName { get; set; }
    public string Length { get; set; }
    public int Bytes { get; set; }
    public decimal UnitPrice { get; set; }

    public override string ToString()
    {
        return SongName;
    }
}

public class Student
{
    public int StudentId { get; set; }
    public int? UserId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Gender { get; set; }
    public string Email { get; set; }
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    public System.DateTime DOB { get; set; }
    public string PlaceOfBirth { get; set; }
    public string Address { get; set; }
    public int CityId { get; set; }
    public int DistrictId { get; set; }
    public int? PinCode { get; set; }
    public byte[] ProfilePicBinary { get; set; }
    public string PhoneNumber { get; set; }
    public bool? Active { get; set; }
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    public System.DateTime? CreatedOnUtc { get; set; }
    public bool? Deleted { get; set; }

    public virtual Academic Academic { get; set; }
    public virtual IList<Enrollment> Enrollments { get; set; }

    public Student()
    {
        Academic = new Academic();
        Enrollments = new List<Enrollment>();
    }
}

public class Academic
{
    public int AcademicId { get; set; }
    public int? StudentId { get; set; }
    public int? TenthMark { get; set; }
    public string TenthSchoolName { get; set; }
    public string TenthSchoolType { get; set; }
    public int? TwelthMark { get; set; }
    public string TwelthSchoolName { get; set; }
    public string TwelthSchoolType { get; set; }
    public int? AcademicGroupId { get; set; }
}

public class Enrollment
{
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public int CollegeId { get; set; }
    public int EnrollmentYear { get; set; }
    public string EnrollmentDesc { get; set; }

    public virtual IList<Semester> Semesters { get; set; }

    public Enrollment()
    {
        Semesters = new List<Semester>();
    }
}

public class Semester
{
    public int SemesterId { get; set; }
    public int EnrollmentId { get; set; }
    public decimal? Grade { get; set; }
    public decimal? Fees { get; set; }
    public string FeesDesc { get; set; }
    public int PaymentMode { get; set; }
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}", ApplyFormatInEditMode = true)]
    public System.DateTime DateOfPayment { get; set; }
    public string ChequeNo { get; set; }
    public string BankDetails { get; set; }
}

public class College
{
    public int CollegeId { get; set; }
    public string CollegeCode { get; set; }
    public string CollegeName { get; set; }
    public string CollegeAddress { get; set; }
    public int DistrictId { get; set; }
}

public class Course
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; }
    public string CourseName { get; set; }
    public string CourseDesc { get; set; }
}

public class District
{
    public int DistrictId { get; set; }
    public string DistrictName { get; set; }
    public int StateId { get; set; }
}

public class User
{
    public int Id { get; set; }

    public string Username { get; set;  }

    public string Password { get; set;  }

    public string Fullname { get; set;  } 
}
}