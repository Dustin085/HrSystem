import { NgModule } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';
import { ReactiveFormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';

import { ApiModule, Configuration } from './api';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { EmployeesPageComponent } from './employees/employees-page.component';
import { ShiftsPageComponent } from './shifts/shifts-page.component';

@NgModule({
  declarations: [
    AppComponent,
    EmployeesPageComponent,
    ShiftsPageComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    ReactiveFormsModule,
    // basePath 設成空字串：請求會打到同源的 /api/...，開發時由 proxy.conf.json 轉給後端。
    // 不設的話，產生的程式碼預設會打 http://localhost/api/...
    ApiModule.forRoot(() => new Configuration({ basePath: '' })),
    AppRoutingModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
