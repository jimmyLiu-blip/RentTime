using DevExpress.XtraEditors;
using RentProject.Shared.UIModels;
using RentProject.UIModels;
using System.Collections.Generic;

namespace RentProject
{
    public partial class Project : XtraForm
    {
        // =========================================================
        // B) 假資料 / 資料來源
        // =========================================================
        private readonly List<LocationItem> _locations = new()
        {
            new LocationItem { Location = "Conducted 1", Area = "WG" },
            new LocationItem { Location = "Conducted 2", Area = "WG" },
            new LocationItem { Location = "Conducted 3", Area = "WG" },
            new LocationItem { Location = "Conducted 4", Area = "WG" },
            new LocationItem { Location = "Conducted 5", Area = "WG" },
            new LocationItem { Location = "Conducted 6", Area = "WG" },
            new LocationItem { Location = "SAC 1", Area = "WG" },
            new LocationItem { Location = "SAC 2", Area = "WG" },
            new LocationItem { Location = "SAC 3", Area = "WG" },
            new LocationItem { Location = "FAC 1", Area = "WG" },
            new LocationItem { Location = "Setup Room 1", Area = "WG" },
            new LocationItem { Location = "Conducted A", Area = "HY" },
            new LocationItem { Location = "Conducted B", Area = "HY" },
            new LocationItem { Location = "Conducted C", Area = "HY" },
            new LocationItem { Location = "Conducted D", Area = "HY" },
            new LocationItem { Location = "Conducted E", Area = "HY" },
            new LocationItem { Location = "Conducted F", Area = "HY" },
            new LocationItem { Location = "SAC C", Area = "HY" },
            new LocationItem { Location = "SAC D", Area = "HY" },
            new LocationItem { Location = "SAC G", Area = "HY" },
            new LocationItem { Location = "FAC A", Area = "HY" },
            new LocationItem { Location = "Setup Room A", Area = "HY" },
        };

        private readonly List<TestModeTestItem> _tests = new()
        {
            new TestModeTestItem { TestMode = "Conducted", TestItem = "Conducted Power"},
            new TestModeTestItem { TestMode = "Conducted", TestItem = "Band Edge"},
            new TestModeTestItem { TestMode = "Conducted", TestItem = "Spurious Emission"},
            new TestModeTestItem { TestMode = "Radiated", TestItem = "Band Edge"},
            new TestModeTestItem { TestMode = "Radiated", TestItem = "Spurious Emission"},
            new TestModeTestItem { TestMode = "Normal", TestItem = "Adaptivity"},
            new TestModeTestItem { TestMode = "Normal", TestItem = "Receiver Blocking"},
            new TestModeTestItem { TestMode = "Normal", TestItem = "DFS"},
            new TestModeTestItem { TestMode = "Normal", TestItem = "PWS"},
            new TestModeTestItem { TestMode = "Setup", TestItem = "Debug"},
        };

        private readonly List<string> _engineers = new()
        {
            "Jimmy",
            "Brian",
            "Tom",
            "Bob",
            "Faker"
        };
    }
}
