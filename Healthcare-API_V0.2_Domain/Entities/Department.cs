using System;
using System.Collections.Generic;

namespace Healthcare_API.Models;

public partial class Department
{
    public long DepartmentID { get; private set; }
    public string DepartmentName { get; private set; }
    public string HeadOfDepartment { get; private set; }
    public DateTime? CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public virtual ICollection<Doctor> Doctors { get; private set; } = new List<Doctor>();

    public Department(string departmentName, string headOfDepartment)
    {
        DepartmentName = departmentName;
        HeadOfDepartment = headOfDepartment;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDepartment(string departmentName, string headOfDepartment)
    {
        DepartmentName = departmentName;
        HeadOfDepartment = headOfDepartment;
        UpdatedAt = DateTime.UtcNow;
    }
}